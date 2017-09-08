using System.Threading.Tasks;
using DGGBot.Utilities;
using DGGBot.Utilities.Attributes;
using Discord.Commands;

namespace DGGBot.Modules
{
    [CompSciChannelOnly]
    public class AskModule : ModuleBase<DggCommandContext>
    {
        [Command("justask")]
        [Alias("ask")]
        [CompSciChannelOnly]
        [ChannelThrottle]
        public async Task JustAsk([Remainder] string unused = null)
        {
            await ReplyAsync("Just ask your question and someone will help if they can.");
        }
    }
}