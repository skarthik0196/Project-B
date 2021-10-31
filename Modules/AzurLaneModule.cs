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
            List<Embed> pages = await m_azurLaneApi.SearchForShip(searchString);

            PagedMessage pagedMessage;

            pagedMessage.startIndex             = 0;
            pagedMessage.pages                  = pages;
            pagedMessage.userData               = null;
            pagedMessage.selectionCallback      = null;
            pagedMessage.respondToMessageEvents = false;
            pagedMessage.restrictToUser         = true;
            pagedMessage.user                   = Context.User;

            await SendPagedMessageAsync(pagedMessage);

            //await ReplyAsync(null, false, embed);
        }

        [Command("ship.skin")]
        [Alias("ship.skins")]
        public async Task SkinSearchAsync([Remainder] string searchString)
        {
            List<Embed> pages = await m_azurLaneApi.GetShipSkins(searchString);

            PagedMessage pagedMessage;

            pagedMessage.startIndex             = 0;
            pagedMessage.pages                  = pages;
            pagedMessage.userData               = null;
            pagedMessage.selectionCallback      = null;
            pagedMessage.respondToMessageEvents = false;
            pagedMessage.restrictToUser         = true;
            pagedMessage.user                   = Context.User;

            await SendPagedMessageAsync(pagedMessage);
        }

        [Command("ship.skill")]
        [Alias("ship.skills")]
        public async Task SkillSearchAsync([Remainder] string searchString)
        {
            Embed embed = await m_azurLaneApi.GetShipSkills(searchString);

            await ReplyAsync(null, false, embed);
        }

        [Command("ship.voicelines")]
        [Alias("ship.voice")]
        public async Task VoiceSearchAsync([Remainder] string searchString)
        {
            List<Embed> pages = await m_azurLaneApi.GetShipVoiceLinePages(searchString);

            PagedMessage pagedMessage;

            pagedMessage.startIndex             = 0;
            pagedMessage.pages                  = pages;
            pagedMessage.userData               = null;
            pagedMessage.selectionCallback      = null;
            pagedMessage.respondToMessageEvents = false;
            pagedMessage.restrictToUser         = true;
            pagedMessage.user                   = Context.User;

            await SendPagedMessageAsync(pagedMessage);
        }
    }
}
