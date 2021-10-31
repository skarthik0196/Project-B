using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using ProjectB.Services;
using Microsoft.Extensions.DependencyInjection;
using JikanDotNet;
using Discord.WebSocket;

namespace ProjectB.Modules
{
    public class MalModule : InteractiveModule
    {
        public readonly MyAnimeListService m_malService;
        static private string s_InformationEmoji = "ℹ️";

        public MalModule(Services.EventHandler embedHandler, MyAnimeListService malService) : base(embedHandler)
        {
            m_malService = malService;
        }

        [Command("anime")]
        public async Task AnimeSearchAsync([Remainder] string searchString)
        {
            List<AnimeSearchEntry> searchResults = await m_malService.SearchForAnime(searchString);

            if (searchResults.Count > 1)
            {
                List<Embed> pagedResults = await Task.Run(() => m_malService.GetPagedEntries(searchResults));

                PagedMessage pagedMessage;
                pagedMessage.startIndex              = 0;
                pagedMessage.pages                  = pagedResults;
                pagedMessage.userData               = searchResults;
                pagedMessage.selectionCallback      = SelectAnimeFromPage;
                pagedMessage.respondToMessageEvents = true;
                pagedMessage.restrictToUser         = true;
                pagedMessage.user                   = Context.User;

                await SendPagedMessageAsync(pagedMessage);
            }
            else if (searchResults.Count == 1)
            {
                Embed embed = await m_malService.GetEmbedMessageFromSearchEntry(searchResults[0], false);

                IUserMessage message = await ReplyAsync(null, false, embed);

                Emoji infoEmoji = new Emoji(s_InformationEmoji);

                await message.AddReactionAsync(infoEmoji);

                List<IEmote> emote = new List<IEmote> { infoEmoji };

                ReactionEventInfo handlerEvent;
                handlerEvent.callback       = DisplayDetailedAnime;
                handlerEvent.message        = message;
                handlerEvent.restrictToUser = false;
                handlerEvent.user           = null;
                handlerEvent.data           = searchResults[0];
                handlerEvent.reactionEmojis = emote;

                m_eventHandler.AddReactionEvent(handlerEvent);
            }
        }

        protected async Task<bool> SelectAnimeFromPage(object obj, IUserMessage userMessage, IUserMessage botMessage, ISocketMessageChannel channel)
        {
            List<AnimeSearchEntry> searchResults = (List<AnimeSearchEntry>)obj;
            bool shouldRequeueEvent              = false;
            Int32 selection;

            if (Int32.TryParse(userMessage.Content, out selection))
            {
                // Display the selected anime if it's in range
                if ((selection > 0) && (selection <= searchResults.Count))
                {
                    Int32 index = selection - 1;
                    Embed embed = await m_malService.GetEmbedMessageFromSearchEntry(searchResults[index], false);

                    await botMessage.RemoveAllReactionsAsync();
                    await botMessage.ModifyAsync(msg => { msg.Embed = embed; });

                    Emoji infoEmoji = new Emoji(s_InformationEmoji);

                    await botMessage.AddReactionAsync(infoEmoji);

                    List<IEmote> emote = new List<IEmote> { infoEmoji };

                    ReactionEventInfo handlerEvent;
                    handlerEvent.callback       = DisplayDetailedAnime;
                    handlerEvent.message        = botMessage;
                    handlerEvent.restrictToUser = false;
                    handlerEvent.user           = null;
                    handlerEvent.data           = searchResults[index];
                    handlerEvent.reactionEmojis = emote;

                    m_eventHandler.AddReactionEvent(handlerEvent);
                }
                // If the user entered a wrong number, it might have been a typo so requeue the event
                else
                {
                    shouldRequeueEvent = true;
                }
            }
            // TODO: Figure out if there's a preference to keep the event alive even if the reply is completely
            // unrelated
            //else
            //{
            //    shouldRequeueEvent = true;
            //}

            return shouldRequeueEvent;
        }

        public async Task DisplayDetailedAnime(IUserMessage message, ISocketMessageChannel channel, SocketReaction reaction, object obj)
        {
            AnimeSearchEntry searchEntry = (AnimeSearchEntry)obj;

            Embed embed = await m_malService.GetEmbedMessageFromSearchEntry(searchEntry, true);

            await message.ModifyAsync(msg => { msg.Embed = embed; });
        }
    }
}
