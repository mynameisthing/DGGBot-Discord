using System;
using System.Text;
using System.Threading.Tasks;
using DGGBot.Utilities;
using DGGBot.Utilities.Attributes;
using Discord.Addons.Interactive;
using Discord.Commands;
using Serilog;

namespace DGGBot.Modules
{
    [Group("wander")]
    [Alias("bot")]
    public class BotModule : InteractiveBase<DggCommandContext>
    {
        [Command]
        public async Task Bot()
        {
            /*<#283012703159844864>*/

            var builder = new StringBuilder();
            builder.AppendLine("Hello <:MLADY:271856532033896448>")
                .AppendLine(
                    "I post in <#356781927836942339> when Destiny goes live on Twitch, posts a new YouTube video, or tweets.")
                .AppendLine("Let Thing know about any issues (Thing#3912 on Discord).")
                .AppendLine("If you want any features added open an issue on Github or PM me")
                .AppendLine("Source: <https://github.com/mynameisthing/DGGBot-Discord>");

            await ReplyAsync(builder.ToString());
        }

        [Command("help")]
        [Alias("command,commands")]
        public async Task CommandList([Remainder] string unused = null)
        {
            await ReplyAsync(
                "!wander: Gets bot info\n" +
                "!live: Gets info about the current or most recent stream\n" +
                "!youtube: Get the most recent YouTube video posted\n" +
                "!twitter: Get the most recent tweet posted\n" +
                "! <:FerretLOL:271856531857735680> : Post a random ferret picture\n" +
                "! <:ASLAN:271856531505545236> : Post a random Aslan picture");
        }

        
        [Command("kill")]
        [RequireOwnerOrAdmin]
        public async Task Kill()
        {
            Log.Information("{user} killed the bot", Context.User.Username);
            await ReplyAsync($"{Context.User.Username} Killed ME. Get the Cops <:WEEWOO:271856532117913600>");
            Environment.Exit(0);
        }
    }
}