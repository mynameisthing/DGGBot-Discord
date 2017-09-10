using System;
using System.Linq;
using System.Threading.Tasks;
using DGGBot.Data;
using DGGBot.Services;
using Discord.Commands;
using Discord.WebSocket;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;

namespace DGGBot.Utilities.Attributes
{
    public class ChannelThrottle : PreconditionAttribute
    {
        //todo make this search id instead of name
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context,
            CommandInfo command,
            IServiceProvider services)
        {
            if ((await context.Client.GetApplicationInfoAsync()).Owner.Username == context.User.Username)
            {
                return PreconditionResult.FromSuccess();
            }

            var user = context.User as SocketGuildUser;
            if (user.Roles.FirstOrDefault(x => x.Name == "Administrator") != null)
                return PreconditionResult.FromSuccess();

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