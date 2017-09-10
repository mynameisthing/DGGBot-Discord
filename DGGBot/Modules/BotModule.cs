using System;
using System.Text;
using System.Threading.Tasks;
using DGGBot.Utilities;
using DGGBot.Utilities.Attributes;
using Discord;
using Discord.Commands;

namespace DGGBot.Modules
{
   
    public class BotModule : ModuleBase<DggCommandContext>
    {
        private readonly CommandService _commandService;
        private readonly IServiceProvider _serviceProvider;

        public BotModule(CommandService commandService, IServiceProvider serviceProvider)
        {
            _commandService = commandService;
            _serviceProvider = serviceProvider;
        }

        [Command("bot"),ChannelThrottle]
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

        [Command("commands"), ChannelThrottle]
        public async Task CommandList([Remainder] string unused = null)
        {
            await ReplyAsync(
                "!bot: Gets bot info\n" +
                "!live: Gets info about the current or most recent stream\n" +
                "!youtube: Get the most recent YouTube video posted\n" +
                "!twitter: Get the most recent tweet posted\n" +
                "! <:FerretLOL:271856531857735680> : Post a random ferret picture\n" +
                "! <:ASLAN:271856531505545236> : Post a random Aslan picture");
        }

        [Command("admincheck")]
        [RequireOwnerOrAdmin]
        public async Task Status(IGuildUser user)
        {
            
            await ReplyAsync(user.GuildPermissions.Administrator.ToString());
            
            //var process = Process.GetCurrentProcess();

            //await ReplyAsync(
            //    $"Job Count:{JobManager.AllSchedules.Count()}\n" +
            //    $"Memory Usage: {process.WorkingSet64}\n");
        }
       
    }
}