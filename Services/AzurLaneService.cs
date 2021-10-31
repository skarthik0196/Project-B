using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Jan0660.AzurAPINet;
using Jan0660.AzurAPINet.Ships;
using Jan0660.AzurAPINet.VoiceLines;
using Jan0660.AzurAPINet.Equipments;

namespace ProjectB.Services
{
    public class AzurLaneService
    {
        private AzurAPIClientOptions         m_azurAPIClientOptions;
        private AzurAPIClient                m_azurAPIClient;
        private readonly IServiceProvider    m_services;
        private readonly DiscordSocketClient m_discordSocketClient;
        private static                       Int32 s_voiceLinesEntriesPerPage = 10;

        public AzurLaneService(IServiceProvider serviceProvider)
        {
            m_discordSocketClient = serviceProvider.GetRequiredService<DiscordSocketClient>();
            m_services = serviceProvider; ;

            m_azurAPIClientOptions = new AzurAPIClientOptions();
            m_azurAPIClientOptions.EnableCaching = true;
            m_azurAPIClientOptions.ClientType = Jan0660.AzurAPINet.ClientType.Web;

            m_azurAPIClient = new AzurAPIClient(m_azurAPIClientOptions);
        }
        public async Task InitializeAsync()
        {
            await m_azurAPIClient.ReloadEverythingAsync();
        }

        // Returns a List of paged embed, the pages contain the same information
        // but flip through the different skins for the ship.
        public async Task<List<Embed>> SearchForShip(string searchString)
        {
            Ship alShip = await Task.Run(() => m_azurAPIClient.getShip(searchString));

            List<Embed> pages = new List<Embed>();

            if (alShip != null)
            {
                StringBuilder rarityString = new StringBuilder(alShip.Rarity);
                rarityString.Append(' ');

                for (UInt32 i = 0; i < alShip.Stars; ++i)
                {
                    rarityString.Append('★');
                }
                // TODO: Fill out Ship parameters
                // Figure out how much detail is required.

                StringBuilder obtainedFromString = new StringBuilder();

                // Ships that were removed from the CN server
                if (alShip.ObtainedFrom.ObtainedFrom != null)
                {
                    obtainedFromString.AppendLine(alShip.ObtainedFrom.ObtainedFrom);
                }

                if (alShip.ObtainedFrom.Maps.Count > 0)
                {
                    obtainedFromString.Append("Map: ");
                    List<ShipObtainedFromMap> mapList = alShip.ObtainedFrom.Maps;

                    // Display the last 5 maps you can get the ship on
                    for (Int32 i = alShip.ObtainedFrom.Maps.Count - 1;
                        ((i >= alShip.ObtainedFrom.Maps.Count - 5) && (i >= 0));
                         i--)
                    {
                        obtainedFromString.Append(mapList[i].Name);
                        obtainedFromString.Append(", ");
                    }

                    // Remove the last two characters
                    obtainedFromString.Remove(obtainedFromString.Length - 2, 2);
                }

                if (alShip.Id.Contains("Plan"))
                {
                    obtainedFromString.Append("Priority Reasearch");
                }

                for (Int32 i = 0; i < alShip.Skins.Length; ++i)
                {
                    EmbedBuilder embedMessageBuilder = new EmbedBuilder();

                    embedMessageBuilder
                        .AddField("Classification", alShip.HullType, true)
                        .AddField("Faction", alShip.Nationality, true)
                        .AddField("Class", alShip.Class, true)
                        .AddField("Rarity", rarityString, true)
                        .AddField("Seiyuu", $"[{alShip.Misc.Voice.Name}]({alShip.Misc.Voice.Url})", true)
                        .AddField("Construction Time", alShip.Construction.ConstructionTime.ToString(), true)
                        .AddField("Retrofit", alShip.Retrofittable, true)
                        .AddField("ID", alShip.Id, true)
                        .AddField("Obtained From", obtainedFromString)
                        .WithTitle(alShip.Names.code)
                        .WithUrl(alShip.WikiUrl)
                        .WithThumbnailUrl(alShip.Skins[i].Chibi)
                        .WithImageUrl(alShip.Skins[i].Image)
                        .WithFooter($"Skin {i + 1}/{alShip.Skins.Length} - {alShip.Skins[i].Name}");

                    pages.Add(embedMessageBuilder.Build());
                }

            }
            else
            {
                EmbedBuilder embedMessageBuilder = new EmbedBuilder
                {
                    Description = "No Results"
                };

                pages.Add(embedMessageBuilder.Build());
            }

            return pages;
        }

        // Return a list of embeds that represent a skin and it's details
        public async Task<List<Embed>> GetShipSkins(string searchString)
        {
            Ship alShip = await Task.Run(() => m_azurAPIClient.getShip(searchString));

            List<Embed> pages = new List<Embed>();

            if (alShip != null)
            {
                foreach (ShipSkin skin in alShip.Skins)
                {
                    EmbedBuilder embedBuilder = new EmbedBuilder();

                    embedBuilder
                        .AddField("Skin Name", skin.Name)
                        .AddField("Cost", (skin.Info.Cost != null) ? skin.Info.Cost : "N/A", true)
                        .AddField("Live 2D", skin.Info.Live2DModel, true)
                        .AddField("Obtained From", skin.Info.ObtainedFrom)
                        .WithImageUrl(skin.Image)
                        .WithThumbnailUrl(skin.Chibi)
                        .WithTitle(alShip.Names.code)
                        .WithUrl(alShip.WikiUrl)
                        .WithFooter($"Skin {pages.Count + 1}/{alShip.Skins.Length} - {skin.Name}");

                    pages.Add(embedBuilder.Build());
                }
            }
            else
            {
                EmbedBuilder embedMessageBuilder = new EmbedBuilder
                {
                    Description = "No Results"
                };

                pages.Add(embedMessageBuilder.Build());
            }

            return pages;
        }

        public async Task<Embed> GetShipSkills(string searchString)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            Ship alShip = await Task.Run(() => m_azurAPIClient.getShip(searchString));

            if (alShip != null)
            {
                embedBuilder
                    .WithTitle(alShip.Names.code)
                    .WithUrl(alShip.WikiUrl)
                    .WithThumbnailUrl(alShip.Thumbnail)
                    .WithFooter($"{alShip.Names.code} Skills, ID: {alShip.Id}");

                foreach (ShipSkill skill in alShip.Skills)
                {
                    embedBuilder.AddField(skill.Names.en, skill.Description);
                }
            }
            else
            {
                embedBuilder.WithDescription("No Results");
            }

            return embedBuilder.Build();
        }

        // Currently only returns a list of embeds for the voice lines of the default/base skin.The API appears to be limited for
        // voice lines and not all of them are available. Some ships (Richelieu for example) return 0 voice lines when
        // queried through the API.
        public async Task<List<Embed>> GetShipVoiceLinePages(string searchString)
        {
            List<Embed> pages = new List<Embed>();

            Ship alShip = m_azurAPIClient.getShip(searchString);

            if (alShip != null)
            {
                Dictionary<string, VoiceLine[]> voiceLinesMap = await Task.Run(() => m_azurAPIClient.getVoiceLinesById(alShip.Id));

                if (voiceLinesMap != null)
                {
                    // Voicelines for default/base skin only
                    VoiceLine[] voiceLineList = voiceLinesMap[alShip.Skins[0].Name] ;

                    Int32 totalPageCount = (voiceLineList.Length / s_voiceLinesEntriesPerPage) + 1;

                    for (Int32 currentPage = 0; currentPage < totalPageCount; currentPage++)
                    {
                        EmbedBuilder embedBuilder = new EmbedBuilder();
                        embedBuilder
                            .WithTitle(alShip.Names.code)
                            .WithUrl(alShip.WikiUrl)
                            .WithThumbnailUrl(alShip.Skins[0].Chibi)
                            .WithFooter($"{alShip.Names.code} | VA: {alShip.Misc.Voice.Name}");

                        for (Int32 i = 0; i < s_voiceLinesEntriesPerPage; i++)
                        {
                            Int32 index = (currentPage * s_voiceLinesEntriesPerPage) + i;

                            if (index < voiceLineList.Length)
                            {
                                string audioString = (voiceLineList[index].Audio != null) ? voiceLineList[index].Audio : "N/A";

                                embedBuilder.AddField(voiceLineList[index].Event, (voiceLineList[index].en != null) ? $"[{voiceLineList[index].en}]({voiceLineList[index].Audio})" : "N/A");
                            }
                        }

                        pages.Add(embedBuilder.Build());
                    }
                }
            }
            
            // No pages were found
            if (pages.Count < 1)
            {
                EmbedBuilder embedMessageBuilder = new EmbedBuilder
                {
                    Description = "No Results"
                };

                pages.Add(embedMessageBuilder.Build());
            }

            return pages;
        }
    }
}
