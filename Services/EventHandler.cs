using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Timers;

namespace ProjectB.Services
{
    using UserMessageCallback = Func<SocketUserMessage, object, IUserMessage, Task>;

    // All events time out after 5 minutes.
    public struct ReactionEventInfo
    {
        // Must be of type
        // async Task Callback(IUserMessage, ISocketMessageChannel socket, SocketReaction reaction)
        public Func<IUserMessage, ISocketMessageChannel, SocketReaction, object, Task> callback;

        // The embed message that will trigger the event when reacted too
        public IUserMessage        message;

        // If the event should only be fired if a specific user reacts
        // Note that reply events require a user to reply
        public bool                restrictToUser;

        // The user to react to, required for ReplyEvents
        public IUser               user;

        public object              data;

        public List<IEmote>        reactionEmojis;
    }

    public struct UserMessageEvent
    {
        public UserMessageCallback callback;

        // User Data
        public object              data;

        // Will be filled out EventHandler
        public DateTime            timeStamp;

        // The message that the user is awaiting a response for
        public IUserMessage        message;
    }

    // All Events are one time only, users must requeue events if they need lasting events (Until you get a vaid response for example).
    // This is done just to make managing lifetime of events easier i.e. can be removed after they are fired.
    // We could rethink this later.
    public class EventHandler
    {
        private readonly IServiceProvider    m_services;
        private readonly DiscordSocketClient m_discordSocketClient;
        private System.Timers.Timer          m_timer;
        private readonly Int32               m_eventTimeoutMinutes = 2; // 2 minutes
        private readonly Int32               m_cleaningEventTickMs = 30 * 1000; // 30 seconds
        private DateTime                     m_previousTimeStamp;

        private ConcurrentDictionary<UInt64, List<ReactionEventInfo>>   m_reactionEvents;
        private ConcurrentDictionary<UInt64, List<UserMessageEvent>>    m_userMessageEvents;
        public EventHandler(IServiceProvider serviceProvider)
        {
            m_discordSocketClient = serviceProvider.GetRequiredService<DiscordSocketClient>();
            m_services            = serviceProvider;

            m_discordSocketClient.ReactionAdded   += OnReactionAdded;
            m_discordSocketClient.MessageReceived += OnMessageReceived;

            m_reactionEvents      = new ConcurrentDictionary<ulong, List<ReactionEventInfo>>();
            m_userMessageEvents = new ConcurrentDictionary<ulong, List<UserMessageEvent>>();
            m_timer               = new System.Timers.Timer();
        }

        public Task InitializeAsync()
        {
            // Initialize the timer to call our cleaning function every 10 seconds.
            var timer = new System.Timers.Timer(m_cleaningEventTickMs);
            timer.Enabled   = true;
            timer.Elapsed  += CleanEvents;
            timer.AutoReset = true;

            m_previousTimeStamp = DateTime.Now;

            return Task.CompletedTask;
        }

        public void AddReactionEvent(ReactionEventInfo handlerEvent)
        {
            bool result = false;
            List<ReactionEventInfo> eventList;

            if (m_reactionEvents.ContainsKey(handlerEvent.message.Id))
            {
                result = m_reactionEvents.TryGetValue(handlerEvent.message.Id, out eventList);

                if (result == true)
                {
                    eventList.Add(handlerEvent);
                }
            }
            else
            {
                eventList = new List<ReactionEventInfo> { handlerEvent };
                result = m_reactionEvents.TryAdd(handlerEvent.message.Id, eventList);
            }

            if (result ==false)
            {
                throw new ArgumentException("Error adding event to Emebed Handler event list");
            }
        }

        public void AddUserMessageEvent(IUser user, UserMessageEvent messageEvent)
        {
            List<UserMessageEvent> userMessageEventList;

            if (m_userMessageEvents.TryGetValue(user.Id, out userMessageEventList) == false)
            {
                userMessageEventList = new List<UserMessageEvent>();

                if (m_userMessageEvents.TryAdd(user.Id, userMessageEventList) == false)
                {
                    throw new Exception("Failed to add user message event");
                }
            }

            messageEvent.timeStamp = DateTime.Now;

            userMessageEventList.Add(messageEvent);
        }

        public async Task OnReactionAdded(Cacheable<IUserMessage, UInt64> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            List<ReactionEventInfo> eventList;

            // Don't react to self
            if (reaction.UserId != m_discordSocketClient.CurrentUser.Id)
            {
                // Fire any Embed Handler events we may have.
                if (m_reactionEvents.TryGetValue(message.Id, out eventList))
                {
                    await FireEvents(eventList, message.Value, channel, reaction);

                    if (eventList.Count < 1)
                    {
                        m_reactionEvents.Remove(message.Id, out eventList);
                    }
                }
            }
        }

        public async Task OnMessageReceived(SocketMessage socketMessage)
        {
            // Only respond to users, Ignore Bots and other system messages
            if ((socketMessage is SocketUserMessage message) &&
               (message.Source == MessageSource.User))
            {
                List<UserMessageEvent> eventList;
                if (m_userMessageEvents.TryRemove(message.Author.Id, out eventList))
                {
                    foreach (UserMessageEvent messageEvent in eventList)
                    {
                        await messageEvent.callback(message, messageEvent.data, messageEvent.message);
                    }

                    eventList.Clear();
                }
            }
        }

        private async Task FireEvents(List<ReactionEventInfo> eventList, IUserMessage message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            for (Int32 i = eventList.Count - 1; i >= 0; --i) 
            {
                ReactionEventInfo fireEvent = eventList[i];

                if ((fireEvent.restrictToUser == false) || (fireEvent.user.Id == reaction.UserId))
                {
                    foreach (IEmote emote in fireEvent.reactionEmojis)
                    {
                        if (reaction.Emote.Name == emote.Name)
                        {
                            await fireEvent.callback((message == null) ? fireEvent.message : message, 
                                                                                             channel,
                                                                                             reaction,
                                                                                             eventList[i].data);

                            lock (eventList)
                            {
                                eventList.RemoveAt(i);
                            }
                            break;
                        }
                    }
                }
            }
        }

        // TODO: Find a better way to clean
        private void CleanEvents(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            List<UInt64> keysToClean = new List<ulong>();

            foreach (KeyValuePair<UInt64, List<ReactionEventInfo>> keyValuePair in m_reactionEvents)
            {
                List<ReactionEventInfo> eventList = keyValuePair.Value;

                lock (eventList)
                {
                    if (eventList.Count > 0)
                    {
                        for (Int32 i = eventList.Count - 1; i >= 0; --i)
                        {
                            ReactionEventInfo embedEvent = eventList[i];

                            TimeSpan duration;

                            if (eventList[i].message.EditedTimestamp.HasValue)
                            {
                                duration = DateTimeOffset.Now.LocalDateTime -
                                           eventList[i].message.EditedTimestamp.Value.LocalDateTime;
                            }
                            else
                            {
                                duration = DateTimeOffset.Now.LocalDateTime -
                                           eventList[i].message.Timestamp.LocalDateTime;
                            }

                            if (duration.TotalMinutes > m_eventTimeoutMinutes)
                            {
                                eventList.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }

                if (eventList.Count < 1)
                {
                    keysToClean.Add(keyValuePair.Key);
                }
            }

            // Remove all stale events
            foreach (UInt64 key in keysToClean)
            {
                List<ReactionEventInfo> eventList;

                m_reactionEvents.Remove(key, out eventList);

                // Check again to make sure no new events were added, if something added between this time
                // add the key back.
                if (eventList != null)
                {
                    lock (eventList)
                    {
                        if (eventList.Count > 0)
                        {
                            m_reactionEvents.TryAdd(key, eventList);
                        }
                    }
                }

            }

            keysToClean.Clear();

            foreach (KeyValuePair<UInt64, List<UserMessageEvent>> keyValuePair in m_userMessageEvents)
            {
                List<UserMessageEvent> eventList = keyValuePair.Value;

                for (Int32 i = eventList.Count - 1; i >= 0; --i)
                {
                    TimeSpan duration = DateTime.Now - eventList[i].timeStamp;

                    if (duration.Minutes >= m_eventTimeoutMinutes)
                    {
                        lock (eventList)
                        {
                            eventList.RemoveAt(i);
                        }
                    }
                }

                if (eventList.Count < 1)
                {
                    keysToClean.Add(keyValuePair.Key);
                }

            }

            foreach (UInt64 key in keysToClean)
            {
                List<UserMessageEvent> eventList;

                m_userMessageEvents.Remove(key, out eventList);

                // Check again to make sure no new events were added, if something added between this time
                // add the key back.
                if (eventList != null)
                {
                    lock (eventList)
                    {
                        if (eventList.Count > 0)
                        {
                            m_userMessageEvents.TryAdd(key, eventList);
                        }
                    }
                }
            }
        }

    }

}
