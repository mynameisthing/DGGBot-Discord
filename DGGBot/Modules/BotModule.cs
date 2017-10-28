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
    
    public class BotModule : InteractiveBase<DggCommandContext>
    {
     

        [Command("help")]
        [Alias("command,commands")]
        public async Task CommandList([Remainder] string unused = null)
        {
            await ReplyAsync(
                
                "!live: Gets info about the current or most recent stream\n" +
                "!youtube: Get the most recent YouTube video posted\n" +
                "!twitter: Get the most recent tweet posted\n" +
                "!ferret : Post a random ferret picture\n" +
                "!aslan : Post a random Aslan picture");
        }

        [Command("game")]
        public async Task SetGame(string gamePlaying)
        {
            await Context.Client.SetGameAsync(gamePlaying);
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