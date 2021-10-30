using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ProjectB.Services;

namespace ProjectB.Modules
{
    public class AzurLaneModule : InteractiveModule
    {
        private readonly AzurLaneService m_azurLaneApi;

        public AzurLaneModule(Services.EventHandler embedHandler, AzurLaneService azurLaneApi) : base(embedHandler)
        {
            m_azurLaneApi = azurLaneApi;
        }

        [Command("ship")]
        public async Task ShipSearchAsync([Remainder] string searchString)
        {
            Embed embed = await m_azurLaneApi.SearchForShip(searchString);

            await ReplyAsync(null, false, embed);
        }

        [Command("skin")]
        [Alias("skins")]
        public async Task SkinSearchAsync([Remainder] string searchString)
        {

        }
    }
}
