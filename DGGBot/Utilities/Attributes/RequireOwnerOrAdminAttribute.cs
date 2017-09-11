using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DGGBot.Utilities.Attributes
{
    public class RequireOwnerOrAdminAttribute : PreconditionAttribute
    {
        //todo make this search id instead of name
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context,
            CommandInfo command,
            IServiceProvider services)
        {
            var user = context.User as IGuildUser;
            if (user.GuildPermissions.Administrator ||
                (await context.Client.GetApplicationInfoAsync()).Owner.Username == user.Username)
            {
                return PreconditionResult.FromSuccess();
            }

            return PreconditionResult.FromError("");
        }
    }
}