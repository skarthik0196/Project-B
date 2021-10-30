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
        private static readonly Int32        s_entriesPerPage = 5;

        private readonly IJikan              m_jikan;
        private readonly IServiceProvider    m_services;
        private readonly DiscordSocketClient m_discordSocketClient;
        public MyAnimeListService(IServiceProvider serviceProvider)
        {
            m_discordSocketClient = serviceProvider.GetRequiredService<DiscordSocketClient>();
            m_jikan               = serviceProvider.GetRequiredService<Jikan>();
            m_services            = serviceProvider;
        }

        public async Task<List<AnimeSearchEntry>> SearchForAnime(string searchString)
        {
            AnimeSearchResult searchResult = await m_jikan.SearchAnime(searchString);

            return searchResult.Results.ToList();
        }

        public async Task<Embed> GetEmbedMessageFromSearchEntry(AnimeSearchEntry searchEntry, bool detailDescription = false)
        {
            Discord.EmbedBuilder embedMessage = new EmbedBuilder();

            Anime anime = await m_jikan.GetAnime(searchEntry.MalId);

            embedMessage
            .AddField("English Title", (anime.TitleEnglish != null) ? anime.TitleEnglish : "N/A")
            .AddField("Episode Count ", (anime.Episodes != null) ? anime.Episodes : "Unknown", true)
            .AddField("Type", (anime.Type != null) ? anime.Type : "Unknown", true)
            .AddField("Status", (anime.Status != null) ? anime.Status : "Unknown", true)
            .AddField("Score", (anime.Score != null) ? anime.Score : "N/A", true)
            .AddField("Rank", (anime.Rank != null) ? anime.Rank : "N/A", true)
            .AddField("Source", (anime.Source != null) ? anime.Source : "Unknown", true)
            .AddField("Genres", GetMalSubItemCollectionString(anime.Genres, ","), true)
            .AddField("MAL ID", anime.MalId, true)
            .WithTitle(anime.Title)
            .WithDescription(searchEntry.Description)
            .WithUrl(anime.LinkCanonical)
            .WithImageUrl(anime.ImageURL)
            .WithFooter(anime.Title);

            if (detailDescription)
            {
                embedMessage
                .AddField("Studios", GetMalSubItemCollectionString(anime.Studios, ","))
                .AddField("Start Date", (anime.Aired.From != null) ? anime.Aired.From.Value.Date.ToLongDateString() : "Unknown", true)
                .AddField("End Date", (anime.Aired.To != null) ? anime.Aired.To.Value.Date.ToLongDateString() : "N/A", true)
                .AddField("Rating", (anime.Rating != null) ? anime.Rating : "Unknown", true)
                .WithDescription(anime.Synopsis);
            }

            return embedMessage.Build();
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

        public List<Embed> GetPagedEntries(List<AnimeSearchEntry> searchEntries)
        {
            List<Embed> pagedEntries = new List<Embed>();

            Int32 totalPages = ((searchEntries.Count - 1) / s_entriesPerPage) + 1;

            for (Int32 pageNum = 0; pageNum < totalPages; ++pageNum)
            {
                EmbedBuilder embedBuilder = new EmbedBuilder();

                embedBuilder
                    .WithTitle("Anime Search Results")
                    .WithFooter($"Page {pageNum + 1}/{totalPages}");

                for (Int32 i = 0; i < s_entriesPerPage; ++i)
                {
                    Int32 index = (pageNum * s_entriesPerPage) + i;

                    embedBuilder.AddField((index + 1).ToString(), $"{searchEntries[index].Title} ({searchEntries[index].Type})");
                }

                pagedEntries.Add(embedBuilder.Build());
            }

            return pagedEntries;
        }
    }
}
