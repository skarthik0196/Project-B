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
                List<String> pagedResults = await Task.Run(() => m_malService.GetPagedEntries(searchResults));

                PagedMessage pagedMessage;
                pagedMessage.entryList         = searchResults.Cast<object>().ToList();
                pagedMessage.pagedString       = pagedResults;
                pagedMessage.title             = "Anime Search Results";
                pagedMessage.startIndex        = 0;
                pagedMessage.selectionCallback = SelectAnimeFromPage;
                pagedMessage.restrictToUser    = true;
                pagedMessage.user              = Context.User;

                await SendPagedMessageAsync(pagedMessage);
            }
            else if (searchResults.Count == 1)
            {

            }
        }

        protected async Task SelectAnimeFromPage(object obj, IUserMessage message, ISocketMessageChannel channel)
        {
            AnimeSearchEntry searchEntry = (AnimeSearchEntry)obj;

            EmbedBuilder builder = await m_malService.GetEmbedMessageFromSearchEntry(searchEntry, false);

            Embed embed = builder.Build();

            await message.ModifyAsync(msg => { msg.Embed = embed; });

            Emoji infoEmoji = new Emoji(s_InformationEmoji);

            await message.AddReactionAsync(infoEmoji);

            List<IEmote> emote = new List<IEmote> { infoEmoji };

            ReactionEventInfo handlerEvent;
            handlerEvent.callback       = DisplayDetailedAnime;
            handlerEvent.message        = message;
            handlerEvent.restrictToUser = false;
            handlerEvent.user           = null;
            handlerEvent.data           = obj;
            handlerEvent.reactionEmojis = emote;

            m_eventHandler.AddReactionEvent(handlerEvent);
        }

        public async Task DisplayDetailedAnime(IUserMessage message, ISocketMessageChannel channel, SocketReaction reaction, object obj)
        {
            AnimeSearchEntry searchEntry = (AnimeSearchEntry)obj;

            EmbedBuilder builder = await m_malService.GetEmbedMessageFromSearchEntry(searchEntry, true);

            Embed embed = builder.Build();

            await message.ModifyAsync(msg => { msg.Embed = embed; });
        }
    }
}
