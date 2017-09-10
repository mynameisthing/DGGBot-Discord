using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DGGBot.Data;
using DGGBot.Services;
using Discord.Commands;
using Discord.WebSocket;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;

namespace DGGBot.Utilities.Attributes
{
    public class RequireOwnerOrAdminAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context,
            CommandInfo command,
            IServiceProvider services)
        {
            var user = context.User as SocketGuildUser;
            if ((await context.Client.GetApplicationInfoAsync()).Owner.Username == user.Username)
            {
                return PreconditionResult.FromSuccess();
            }
            if (user.Roles.FirstOrDefault(x => x.Name == "Administrator") != null)
            {
                return PreconditionResult.FromSuccess();
            }
            return PreconditionResult.FromError("");
        }
    }
}
