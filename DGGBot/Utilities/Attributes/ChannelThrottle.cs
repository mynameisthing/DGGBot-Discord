using System;
using System.Threading.Tasks;
using DGGBot.Data;
using DGGBot.Services;
using Discord.Commands;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;

namespace DGGBot.Utilities.Attributes
{
    public class ChannelThrottle : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context,
            CommandInfo command,
            IServiceProvider services)
        {
            using (var dggContext = new DggContext())
            {
                var throttle = await dggContext.Throttles.FirstOrDefaultAsync(x =>
                    x.DiscordChannelId == context.Channel.Id && x.ModuleName == command.Module.Name);
                if (throttle != null)
                    return PreconditionResult.FromError("");

                JobManager.AddJob(new ThrottleJob(command.Module.Name, context.Channel.Id), s => s.ToRunNow());
                return PreconditionResult.FromSuccess();
            }
        }
    }
}