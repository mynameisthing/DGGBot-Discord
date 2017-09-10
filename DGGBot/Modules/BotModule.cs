﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DGGBot.Utilities;
using Discord.Commands;

namespace DGGBot.Modules
{
    [Group("bot")]
    public class BotModule : ModuleBase<DggCommandContext>
    {
        private readonly CommandService _commandService;
        private readonly IServiceProvider _serviceProvider;

        public BotModule(CommandService commandService,IServiceProvider serviceProvider)
        {
            _commandService = commandService;
            _serviceProvider = serviceProvider;
        }
        [Command]
        public async Task Bot()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Hello <:MLADY:271856532033896448>")
                .AppendLine(
                    "I post in <#283012703159844864> when Destiny goes live on Twitch, posts a new YouTube video, or tweets.")
                .AppendLine("Let Thing know about any issues (Thing#3912 on Discord).")
                .AppendLine("If you want any features added open an issue on Github")
                .AppendLine("Source: <https://github.com/mynameisthing/DGGBot-Discord>");
                    
            await ReplyAsync(builder.ToString());
        }
    }
}
