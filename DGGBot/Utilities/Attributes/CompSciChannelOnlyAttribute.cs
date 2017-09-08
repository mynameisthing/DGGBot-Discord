using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace DGGBot.Utilities.Attributes
{
    public class CompSciChannelOnlyAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context,
            CommandInfo command,
            IServiceProvider services)
        {
            //353286877866098700
            //273920762312916992

            if (context.Channel.Id == 273920762312916992)
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(PreconditionResult.FromError(""));
        }
    }
}