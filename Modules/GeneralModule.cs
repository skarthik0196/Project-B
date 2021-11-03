using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using ProjectB;
using ProjectB.Services;

namespace ProjectB.Modules
{
    public class GeneralModule : ModuleBase<SocketCommandContext>
    {
        [Command("hello")]
        public async Task HelloAsync()
        {
            await ReplyAsync("Hello World");
        }

        [Command("help")]
        public async Task HelpAsync()
        {
            // Probably more optimal to write it as a single string
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("Commands for Project B");
            stringBuilder.AppendLine("Anything within [] in the text below means it must be replaced with your input (Do not include the `[]`)\n\n\n");
            stringBuilder.AppendLine("The prefix for ProjectB is `x.` that means all commands below must be prefixed with `x.`, for example `x.command input` \n\n");
            stringBuilder.AppendLine("`anime [AnimeName]` - Searches for an anime by quering the MAL API, be warned that the MAL API can get rate limited if too many requests are made and might return a connection error. Until a suitable caching mechanism is implemented refraining from spamming searches and use only when necessary \n\n");
            stringBuilder.AppendLine("`ship [ShipName]` - Display information for the given AzurLane ship, note that the name must match exactly.\n\n");
            stringBuilder.AppendLine("`ship.list [FactionPrefix (or) FactionFullName]` - Search all ships in a given faction and display a paged result. You may change pages by using the buttons and select a ship by typing the number displayed.\n\n");
            stringBuilder.AppendLine("`ship.skin [ShipName]` - Display all skins for the given ship with skin information. Ship name must be exact\n\n");
            stringBuilder.AppendLine("`ship.skill [ShipName]` - Display all skills for the given ship. Ship name must be exact\n\n");
            stringBuilder.AppendLine("`ship.voicelines [ShipName]` - Display all voicelines for the default skin of a given ship. Ship name must be exact\n\n");
            stringBuilder.AppendLine("`ship.equipment [EquipmentName]` - Display details for the given equipment, name must match\n\n");
            stringBuilder.AppendLine("`ship.equipment.list [EquipmentType (or) Faction Prefix]` - List all equipment in the given equipment type or all equipment belonging to a faction. Selections can be made by typing the displayed number. Note that some equipment types are currently unavailable due to API limitations\n\n");

            EmbedBuilder embedBuilder = new EmbedBuilder
            {
                Title = "Project B Commands",
                Description = stringBuilder.ToString()
            };

            await ReplyAsync(null, false, embedBuilder.Build());
        }
    }
}
