using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DGGBot.Services.Twitter;
using Discord.Commands;
using Microsoft.Extensions.Configuration;

namespace DGGBot.Modules
{
    [Group("add")]
    [RequireOwner]
    public class AdminModule : ModuleBase
    {
        private readonly IConfiguration _config;
        private readonly TwitterService _twitterService;

        public AdminModule(IConfiguration config,TwitterService twitterService)
        {
            _config = config;
            _twitterService = twitterService;
        }
        [Command("twitter")]
        public async Task Twitter(string twitterName,long channelId)
        {
           
           var user = await _twitterService.GetUser(twitterName);
            if (user is null)
            {
                await ReplyAsync("Unable to get user info for the UserName");
                return;
            }
            user.DiscordChannelId = channelId;
            user.DiscordServerId = (long) Context.Guild.Id;
            await ReplyAsync("I did something");
        }

    }
}
