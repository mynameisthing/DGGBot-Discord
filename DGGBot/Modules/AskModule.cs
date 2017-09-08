using System.Threading.Tasks;
using DGGBot.Utilities.Attributes;
using Discord.Commands;

namespace DGGBot.Modules
{
    [CompSciChannelOnly]
    public class AskModule : ModuleBase
    {
        [Command("justask")]
        [Alias("ask")]
        [ChannelThrottle]
        public async Task JustAsk([Remainder] string unused = null)
        {
            await ReplyAsync("Just ask your question and someone will help if they can.");
        }
    }
}