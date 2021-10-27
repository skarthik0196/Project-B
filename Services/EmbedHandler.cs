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
    // All events time out after 5 minutes.
    public struct EmbedHandlerEvent
    {
        // Must be of type
        // async Task Callback(IUserMessage, ISocketMessageChannel socket, SocketReaction reaction)
        public Func<IUserMessage, ISocketMessageChannel, SocketReaction, object, Task> callback;

        // The embed message that will trigger the event when reacted too
        public IUserMessage        message;

        // If the event should only be fired if a specific user reacts
        public bool                restrictToUser;

        // The user to react to, can be null if reaction is not restricted to user.
        public IUser               user;

        public object              data;

        public List<IEmote>        reactionEmojis;
    }

    public class EmbedHandler
    {
        private readonly IServiceProvider    m_services;
        private readonly DiscordSocketClient m_discordSocketClient;
        private System.Timers.Timer          m_timer;
        private readonly Int32               m_eventTimeoutMinutes = 2; // 2 minutes
        private readonly Int32               m_cleaningEventTickMs = 30 * 1000; // 30 seconds

        private ConcurrentDictionary<UInt64, List<EmbedHandlerEvent>> m_embedEvents;

        public EmbedHandler(IServiceProvider serviceProvider)
        {
            m_discordSocketClient = serviceProvider.GetRequiredService<DiscordSocketClient>();
            m_services            = serviceProvider;

            m_discordSocketClient.ReactionAdded += OnReactionAdded;

            m_embedEvents = new ConcurrentDictionary<ulong, List<EmbedHandlerEvent>>();
            m_timer       = new System.Timers.Timer();
        }

        public Task InitializeAsync()
        {
            // Initialize the timer to call our cleaning function every 10 seconds.
            var timer = new System.Timers.Timer(m_cleaningEventTickMs);
            timer.Enabled   = true;
            timer.Elapsed  += CleanEvents;
            timer.AutoReset = true;

            return Task.CompletedTask;
        }

        public void AddOneTimeEvent(EmbedHandlerEvent handlerEvent)
        {
            bool result = false;
            List<EmbedHandlerEvent> eventList;

            if (m_embedEvents.ContainsKey(handlerEvent.message.Id))
            {
                result = m_embedEvents.TryGetValue(handlerEvent.message.Id, out eventList);

                if (result == true)
                {
                    eventList.Add(handlerEvent);
                }
            }
            else
            {
                eventList = new List<EmbedHandlerEvent> { handlerEvent };
                result = m_embedEvents.TryAdd(handlerEvent.message.Id, eventList);
            }

            if (result ==false)
            {
                throw new ArgumentException("Error adding event to Emebed Handler event list");
            }
        }

        public async Task OnReactionAdded(Cacheable<IUserMessage, UInt64> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            List<EmbedHandlerEvent> eventList;

            // Don't react to self
            if (reaction.UserId != m_discordSocketClient.CurrentUser.Id)
            {
                // Fire any Embed Handler events we may have.
                if (m_embedEvents.TryGetValue(message.Id, out eventList))
                {
                    await FireEvents(eventList, message.Value, channel, reaction);

                    lock (eventList)
                    {
                        if (eventList.Count < 1)
                        {
                            m_embedEvents.Remove(message.Id, out eventList);
                        }
                    }
                }
            }
        
        }

        private async Task FireEvents(List<EmbedHandlerEvent> eventList, IUserMessage message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            for (Int32 i = eventList.Count - 1; i >= 0; --i) 
            {
                EmbedHandlerEvent fireEvent = eventList[i];

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

        // Should we just change this into an async function that is part of the regular loop?
        // Maybe once we add message based reaction.
        private void CleanEvents(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            List<UInt64> keysToClean = new List<ulong>();

            foreach (KeyValuePair<UInt64, List<EmbedHandlerEvent>> keyValuePair in m_embedEvents)
            {
                List<EmbedHandlerEvent> eventList = keyValuePair.Value;

                lock (eventList)
                {
                    if (eventList.Count > 0)
                    {
                        for (Int32 i = eventList.Count - 1; i >= 0; --i)
                        {
                            EmbedHandlerEvent embedEvent = eventList[i];

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
                List<EmbedHandlerEvent> eventList;

                m_embedEvents.Remove(key, out eventList);

                // Check again to make sure no new events were added, if something added between this time
                // add the key back.
                if (eventList != null)
                {
                    lock (eventList)
                    {
                        if (eventList.Count > 0)
                        {
                            m_embedEvents.TryAdd(key, eventList);
                        }
                    }
                }

            }
        }

    }

}
