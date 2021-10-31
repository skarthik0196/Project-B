﻿using System;
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
    }
}