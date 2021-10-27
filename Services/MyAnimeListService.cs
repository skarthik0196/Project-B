using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JikanDotNet;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace ProjectB.Services
{
    public class MyAnimeListService
    {
        private readonly IJikan m_jikan;
        private readonly IServiceProvider m_services;
        private readonly DiscordSocketClient m_discordSocketClient;
        private readonly EmbedHandler m_embedHandler;

        public MyAnimeListService(IServiceProvider serviceProvider)
        {
            m_discordSocketClient = serviceProvider.GetRequiredService<DiscordSocketClient>();
            m_jikan               = serviceProvider.GetRequiredService<Jikan>();
            m_services            = serviceProvider;
        }

        public async Task<List<AnimeSearchEntry>> SearchForAnime(string searchString)
        {
            JikanDotNet.AnimeSearchResult searchResult = await m_jikan.SearchAnime(searchString);

            return searchResult.Results.ToList();
        }

        public async Task<Discord.EmbedBuilder> GetEmbedMessageFromSearchEntry(AnimeSearchEntry searchEntry, bool detailDescription = false)
        {
            Discord.EmbedBuilder embedMessage = new EmbedBuilder();

            Anime anime = await m_jikan.GetAnime(searchEntry.MalId);

            embedMessage.AddField("Episode Count ", (anime.Episodes != null) ? anime.Episodes : "Unknown")
            .AddField("Type", (anime.Type != null) ? anime.Type : "Unknown")
            .AddField("Score", (anime.Score != null) ? anime.Score : "N/A")
            .AddField("Status", (anime.Status != null) ? anime.Status : "Unknown")
            .AddField("Genres", GetMalSubItemCollectionString(anime.Genres, ","))
            .AddField("Source", (anime.Source != null) ? anime.Source : "Unknown")
            .AddField("English Title", (anime.TitleEnglish != null) ? anime.TitleEnglish : "N/A")
            .AddField("MAL ID", anime.MalId)
            .WithTitle(anime.Title)
            .WithDescription(searchEntry.Description)
            .WithUrl(anime.LinkCanonical)
            .WithImageUrl(anime.ImageURL)
            .WithFooter(anime.Title);

            if (detailDescription)
            {
                embedMessage
                .AddField("Rank", (anime.Rank != null) ? anime.Rank : "N/A")
                .AddField("Studios", GetMalSubItemCollectionString(anime.Studios, ","))
                .AddField("Start Date", (anime.Aired.From != null) ? anime.Aired.From.Value.Date.ToLongDateString() : "Unknown")
                .AddField("End Date", (anime.Aired.To != null) ? anime.Aired.To.Value.Date.ToLongDateString() : "N/A")
                .AddField("Rating", (anime.Rating != null) ? anime.Rating : "Unknown")
                .WithDescription(anime.Synopsis);
            }

            return embedMessage;
        }

        // Returns a single string with the names of every individual entity in a MALSubItem Collection delimited by the specified delimiter
        private string GetMalSubItemCollectionString(ICollection<MALSubItem> subItemCollection, string delimiter)
        {
            StringBuilder fullString = new StringBuilder("", 50);

            foreach (MALSubItem item in subItemCollection)
            {
                fullString.Append(item.Name);
                fullString.Append(delimiter);
                fullString.Append(" ");
            }

            // Remove the last two characters
            fullString.Remove(fullString.Length - 2, 2);

            return fullString.ToString();
        }

        public List<String> GetPagedEntries(List<AnimeSearchEntry> searchEntries)
        {
            List<String> pagedEntries = new List<String>();

            foreach (AnimeSearchEntry searchEntry in searchEntries)
            {
                pagedEntries.Add($"{searchEntry.Title} ({searchEntry.Type})");
            }

            return pagedEntries;
        }
    }
}
