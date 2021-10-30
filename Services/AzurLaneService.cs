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

namespace ProjectB.Services
{
    public class AzurLaneService
    {
        private AzurAPIClientOptions m_azurAPIClientOptions;
        private AzurAPIClient m_azurAPIClient;
        private readonly IServiceProvider m_services;
        private readonly DiscordSocketClient m_discordSocketClient;

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

        public async Task<Embed> SearchForShip(string searchString)
        {
            Ship alShip = await Task.Run(() => m_azurAPIClient.getShip(searchString));

            EmbedBuilder embedMessage = new EmbedBuilder
            {
                Title = "No Results"
            };

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

                embedMessage
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
                    .WithThumbnailUrl(alShip.Skins[0].Chibi)
                    .WithImageUrl(alShip.Skins[0].Image)
                    .WithFooter($"Skin 1/{alShip.Skins.Length} - {alShip.Skins[0].Name}");
            }

            return embedMessage.Build();
        }

        public async Task<List<ShipSkin>> SearchForSkins(string searchString)
        {
            Ship alShip = await Task.Run(() => m_azurAPIClient.getShip(searchString));

            return alShip.Skins.ToList();
        }
    }
}
