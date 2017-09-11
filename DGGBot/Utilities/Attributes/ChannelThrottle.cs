using System;
using System.Threading.Tasks;
using DGGBot.Data;
using DGGBot.Services;
using Discord;
using Discord.Commands;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;
using Serilog;

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

            var user = context.User as IGuildUser;

            if (user.GuildPermissions.Administrator)
            {
                return PreconditionResult.FromSuccess();
            }

            using (var dggContext = new DggContext())
            {
                var throttle = await dggContext.Throttles.FirstOrDefaultAsync(x => x.DiscordChannelId == context.Channel.Id && x.CommandName == command.Name);
                if (throttle != null)
                {
                    Log.Information("{user} was throttled in {channel} for {commandName} command", user.Username, context.Channel.Name, command.Name);
                    return PreconditionResult.FromError("");
                }
                Log.Information("{user} started throttle in {channel} for {commandName} command",user.Username,context.Channel.Name,command.Name);
                JobManager.AddJob(new ThrottleJob(command.Name, context.Channel.Id), s => s.ToRunNow());
                return PreconditionResult.FromSuccess();
            }
        }
    }
}