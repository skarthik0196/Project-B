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

        [RequireBotPermission(ChannelPermission.AddReactions)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        [RequireBotPermission(ChannelPermission.ViewChannel)]
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

        [Command("ship.list")]
        [RequireBotPermission(ChannelPermission.AddReactions)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        [RequireBotPermission(ChannelPermission.ViewChannel)]
        public async Task ShipListAsync([Remainder] string searchString)
        {
            Tuple<List<string>, List<Embed>> pages = await m_azurLaneApi.GetShipList(searchString);

            PagedMessage pagedMessage;
            
            pagedMessage.startIndex             = 0;
            pagedMessage.pages                  = pages.Item2;
            pagedMessage.userData               = pages.Item1;
            pagedMessage.selectionCallback      = ShipSelectionEvent;
            pagedMessage.respondToMessageEvents = true;
            pagedMessage.restrictToUser         = true;
            pagedMessage.user                   = Context.User;

            await SendPagedMessageAsync(pagedMessage);
        }

        public async Task<bool> ShipSelectionEvent(object userData, IUserMessage userMessage, IUserMessage botMessage, ISocketMessageChannel channel)
        {
            List<string> entryListAsString = (List<string>)userData;
            bool shouldRequeueEvent = false;
            Int32 selection;

            if (Int32.TryParse(userMessage.Content, out selection))
            {
                // Selection is from [1,Count]
                if (selection > 0 && selection <= entryListAsString.Count)
                {
                    await ShipSearchAsync(entryListAsString[selection - 1]);
                }
                // Requeue the event if it looks like they might have entered the wrong number
                else
                {
                    shouldRequeueEvent = true;
                }
            }

            return shouldRequeueEvent;
        }

        [Command("ship.skin")]
        [Alias("ship.skins")]
        [RequireBotPermission(ChannelPermission.AddReactions)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        [RequireBotPermission(ChannelPermission.ViewChannel)]
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
        [RequireBotPermission(ChannelPermission.AddReactions)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        [RequireBotPermission(ChannelPermission.ViewChannel)]
        public async Task SkillSearchAsync([Remainder] string searchString)
        {
            Embed embed = await m_azurLaneApi.GetShipSkills(searchString);

            await ReplyAsync(null, false, embed);
        }

        [Command("ship.voicelines")]
        [Alias("ship.voice")]
        [RequireBotPermission(ChannelPermission.AddReactions)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        [RequireBotPermission(ChannelPermission.ViewChannel)]
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

        [Command("ship.equipment")]
        [Alias("ship.equip")]
        [RequireBotPermission(ChannelPermission.AddReactions)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        [RequireBotPermission(ChannelPermission.ViewChannel)]
        public async Task EquipmentSearchAsync([Remainder] string searchString)
        {
            Embed embed = await m_azurLaneApi.GetEquipment(searchString);

            await ReplyAsync(null, false, embed);
        }

        [Command("ship.equipment.list")]
        [Alias("ship.equip.list")]
        [RequireBotPermission(ChannelPermission.AddReactions)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        [RequireBotPermission(ChannelPermission.ViewChannel)]
        public async Task EquipmentListAsync([Remainder] string searchString)
        {
            Tuple<List<string>, List<Embed>> pages = await m_azurLaneApi.GetEquipmentList(searchString);

            PagedMessage pagedMessage;

            pagedMessage.startIndex             = 0;
            pagedMessage.pages                  = pages.Item2;
            pagedMessage.userData               = pages.Item1;
            pagedMessage.selectionCallback      = EquipmentSelectionEvent;
            pagedMessage.respondToMessageEvents = true;
            pagedMessage.restrictToUser         = true;
            pagedMessage.user                   = Context.User;

            await SendPagedMessageAsync(pagedMessage);
        }

        public async Task<bool> EquipmentSelectionEvent(object userData, IUserMessage userMessage, IUserMessage botMessage, ISocketMessageChannel channel)
        {
            List<string> entryListAsString = (List<string>)userData;
            bool shouldRequeueEvent = false;
            Int32 selection;

            if (Int32.TryParse(userMessage.Content, out selection))
            {
                // Selection is from [1,Count]
                if (selection > 0 && selection <= entryListAsString.Count)
                {
                    await EquipmentSearchAsync(entryListAsString[selection - 1]);
                }
                // Requeue the event if it looks like they might have entered the wrong number
                else
                {
                    shouldRequeueEvent = true;
                }
            }

            return shouldRequeueEvent;
        }
    }
}
