﻿using System;
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
        // The original data collection, this will be passed back to the callback to interpret once a value is chosen
        public List<object> entryList;

        // Entries in the sepraate pages of the paged message
        public List<String> pagedString;

        // Callback function when a specific entry in the paged message is selected
        public Func<object, IUserMessage, ISocketMessageChannel, Task> selectionCallback;

        public String title;

        // The current page of the message, set this on creation to start the message at a different index
        public Int32 startIndex;

        // Restrict response to this Paged Message to a single user.
        public bool  restrictToUser;

        // The user to restrict response too.
        public IUser user;
    }

    public class InteractiveModule : ModuleBase<SocketCommandContext>
    {
        public readonly EmbedHandler     m_embedHandler;

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
        private const string Number1Unicode = "\u0031\u20E3";
        private const string Number2Unicode = "\u0032\u20E3";
        private const string Number3Unicode = "\u0033\u20E3";
        private const string Number4Unicode = "\u0034\u20E3";
        private const string Number5Unicode = "\u0035\u20E3";

        private static string[] s_EmojiArray = { LeftArrowUnicode,
                                                 RightArrowUnicode,
                                                 Number1Unicode,
                                                 Number2Unicode,
                                                 Number3Unicode,
                                                 Number4Unicode,
                                                 Number5Unicode };

        private static Int32 entriesPerPage = 5;

        protected InteractiveModule(EmbedHandler embedHandler)
        {
            m_embedHandler = embedHandler;
        }

        public async Task SendPagedMessageAsync(PagedMessage pagedMessage)
        {
            Embed embed = GetEmbedPage(pagedMessage);

            IUserMessage embedMessage = await ReplyAsync(null, false, embed);

            // Unused, don't need to wait
            Task task = QueuePagingEvent(pagedMessage, embedMessage);
        }

        private Embed GetEmbedPage(PagedMessage pagedMessage)
        {
            EmbedBuilder builder = new EmbedBuilder();

            Int32 page = (pagedMessage.startIndex) / entriesPerPage;

            builder.WithTitle(pagedMessage.title)
                   .WithFooter($"Page {page + 1}/{(pagedMessage.pagedString.Count / entriesPerPage)}");

            pagedMessage.startIndex = page * entriesPerPage;

            for (Int32 i = 0; i < entriesPerPage; ++i)
            {
                builder.AddField($"{(i + 1).ToString()}", pagedMessage.pagedString[pagedMessage.startIndex + i].ToString());
            }

            return builder.Build();
        }

        private async Task QueuePagingEvent(PagedMessage pagedMessageData, IUserMessage embedMessage)
        {
            await embedMessage.RemoveAllReactionsAsync();

            IEmote[] reactions = new IEmote[]
            {
                new Emoji(s_EmojiArray[((int)EmojiType.LeftArrow)]),
                new Emoji(s_EmojiArray[((int)EmojiType.RightArrow)]),
                new Emoji(s_EmojiArray[((int)EmojiType.Number1)]),
                new Emoji(s_EmojiArray[((int)EmojiType.Number2)]),
                new Emoji(s_EmojiArray[((int)EmojiType.Number3)]),
                new Emoji(s_EmojiArray[((int)EmojiType.Number4)]),
                new Emoji(s_EmojiArray[((int)EmojiType.Number5)]),
            };


            // Send the reaction Async, waiting here with await seems to block the gateway for some reason
            // Just send the reaction async and don't wait.
            Task reactionTask = embedMessage.AddReactionsAsync(reactions);

            EmbedHandlerEvent reactionEvent;

            reactionEvent.restrictToUser = pagedMessageData.restrictToUser;
            reactionEvent.message        = embedMessage;
            reactionEvent.callback       = PagedEmbedReactionUpdateAsync;
            reactionEvent.user           = pagedMessageData.user;
            reactionEvent.data           = pagedMessageData;
            reactionEvent.reactionEmojis = reactions.ToList();

            m_embedHandler.AddOneTimeEvent(reactionEvent);
        }

        public async Task PagedEmbedReactionUpdateAsync(IUserMessage message, ISocketMessageChannel channel, SocketReaction reaction, object data)
        {
            PagedMessage pagedData  = (PagedMessage) data;
            bool shouldRequeueEvent = false;

            switch (reaction.Emote.Name)
            {
                case LeftArrowUnicode:
                {
                    if (pagedData.startIndex >= entriesPerPage)
                    {
                        pagedData.startIndex -= entriesPerPage;
                    }
                    shouldRequeueEvent = true;
                    break;
                }
                case RightArrowUnicode:
                {
                    if (pagedData.startIndex < (pagedData.entryList.Count - entriesPerPage))
                    {
                        pagedData.startIndex += entriesPerPage;
                    }
                    shouldRequeueEvent = true;
                    break;
                }
                case Number1Unicode:
                {
                    break;
                }
                case Number2Unicode:
                {
                    pagedData.startIndex += 1;
                    break;
                }
                case Number3Unicode:
                {
                    pagedData.startIndex += 2;
                    break;
                }
                case Number4Unicode:
                {
                    pagedData.startIndex += 3;
                    break;
                }
                case Number5Unicode:
                {
                    pagedData.startIndex += 4;
                    break;
                }
                default:
                {
                    // We should never hit this case
                    break;
                }
            }

            // We are moving to a different page
            if (shouldRequeueEvent)
            {
                Embed embed = GetEmbedPage(pagedData);
                await message.ModifyAsync(msg => { msg.Embed = embed; });

                Task task = QueuePagingEvent(pagedData, message);
            }
            else
            {
                // Something was chosen, remove all reactions and call the callback function
                await message.RemoveAllReactionsAsync();
                await pagedData.selectionCallback(pagedData.entryList[pagedData.startIndex], message, channel);
            }
        }
    }
}
