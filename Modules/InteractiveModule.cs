using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ProjectB.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectB.Modules
{
    public struct PagedMessage
    {
        // The current page of the message, set this on creation to start the message at a different index
        public Int32 startPage;

        // The original data collection, this will be passed back to the callback to interpret once a value is chosen
        public List<Embed> pages;

        // Data passed back to the user if the callback is used
        public object userData;

        // Callback function when a specific entry in the paged message is selected
        // Function must returns a boolean to determine whether the user event must be requeued
        public Func<object, IUserMessage, IUserMessage, ISocketMessageChannel, Task<bool>> selectionCallback;

        // If the client is expecting the user to respond via message
        public bool respondToMessageEvents;

        // Restrict response to this Paged Message to a single user.
        public bool  restrictToUser;

        // The user to restrict response too.
        public IUser user;
    }

    public class InteractiveModule : ModuleBase<SocketCommandContext>
    {
        public readonly Services.EventHandler     m_eventHandler;

        private enum EmojiType
        {
            LeftArrow,
            RightArrow,
            Number1,
            Number2,
            Number3,
            Number4,
            Number5
        }

        private const string LeftArrowUnicode  = "⬅️";
        private const string RightArrowUnicode = "➡️";

        private static string[] s_EmojiArray = { 
                                                 LeftArrowUnicode,
                                                 RightArrowUnicode,
                                               };

        protected InteractiveModule(Services.EventHandler embedHandler)
        {
            m_eventHandler = embedHandler;
        }

        public async Task SendPagedMessageAsync(PagedMessage pagedMessage)
        {
            IUserMessage embedMessage = await ReplyAsync(null, false, pagedMessage.pages[pagedMessage.startPage]);

            // Unused, don't need to wait
            Task task = QueuePagingEvent(pagedMessage, embedMessage);
        }

        private async Task QueuePagingEvent(PagedMessage pagedMessageData, IUserMessage embedMessage)
        {
            await embedMessage.RemoveAllReactionsAsync();

            IEmote[] reactions = new IEmote[]
            {
                new Emoji(s_EmojiArray[((int)EmojiType.LeftArrow)]),
                new Emoji(s_EmojiArray[((int)EmojiType.RightArrow)])
            };

            // Should we skip the await and just continue?
            await embedMessage.AddReactionsAsync(reactions);

            ReactionEventInfo reactionEvent;

            reactionEvent.restrictToUser = pagedMessageData.restrictToUser;
            reactionEvent.message        = embedMessage;
            reactionEvent.callback       = PagedEmbedReactionUpdateAsync;
            reactionEvent.user           = pagedMessageData.user;
            reactionEvent.data           = pagedMessageData;
            reactionEvent.reactionEmojis = reactions.ToList();

            m_eventHandler.AddReactionEvent(reactionEvent);

            UserMessageEvent messageEvent;

            messageEvent.callback  = PagedEmbedResponseEventAsync;
            messageEvent.data      = pagedMessageData;
            messageEvent.timeStamp = DateTime.Now;
            messageEvent.message   = embedMessage;

            m_eventHandler.AddUserMessageEvent(pagedMessageData.user, messageEvent);
        }

        public async Task PagedEmbedReactionUpdateAsync(IUserMessage message, ISocketMessageChannel channel, SocketReaction reaction, object data)
        {
            PagedMessage pagedData  = (PagedMessage) data;
            bool shouldRequeueEvent = false;

            switch (reaction.Emote.Name)
            {
                case LeftArrowUnicode:
                {
                    pagedData.startPage--;
                    Math.Clamp(pagedData.startPage, 0, pagedData.pages.Count - 1);
                    shouldRequeueEvent = true;
                    break;
                }
                case RightArrowUnicode:
                {
                    pagedData.startPage++;
                    Math.Clamp(pagedData.startPage, 0, pagedData.pages.Count - 1);
                    shouldRequeueEvent = true;
                    break;
                }
                default:
                {
                    // We should never hit this case because event handler already handles the reactions
                    break;
                }
            }

            // We are moving to a different page
            if (shouldRequeueEvent)
            {
                //Embed embed = GetEmbedPage(pagedData);
                await message.ModifyAsync(msg => { msg.Embed = pagedData.pages[pagedData.startPage]; });

                Task task = QueuePagingEvent(pagedData, message);
            }
        }

        public async Task PagedEmbedResponseEventAsync(SocketUserMessage userMessage, IUserMessage originalBotMessage, object data, DateTime timeStamp)
        {
            PagedMessage pagedData = (PagedMessage)data;

            bool shouldRequeueEvent = false;

            // If the user replied in a different channel, requeue the event and wait.
            if (userMessage.Channel.Id != originalBotMessage.Channel.Id)
            {
                shouldRequeueEvent = true;
            }
            else
            {
                // The user replied in the channel and it might be a reply to our message
                // Call the client so they can handle it.
                shouldRequeueEvent = await pagedData.selectionCallback(pagedData.userData, userMessage, originalBotMessage, (ISocketMessageChannel)originalBotMessage.Channel);
            }

            if (shouldRequeueEvent)
            {
                UserMessageEvent messageEvent;

                messageEvent.callback  = PagedEmbedResponseEventAsync;
                messageEvent.data      = pagedData;
                messageEvent.timeStamp = timeStamp;
                messageEvent.message   = originalBotMessage;

                m_eventHandler.AddUserMessageEvent(pagedData.user, messageEvent);
            }
        }
    }
}
