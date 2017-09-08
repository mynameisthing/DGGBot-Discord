using System.Linq;
using System.Threading.Tasks;
using DGGBot.Data;
using DGGBot.Data.Enitities;
using DGGBot.Services.Twitter;
using DGGBot.Services.Youtube;
using DGGBot.Utilities;
using Discord.Commands;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DGGBot.Modules
{
    [Group("add")]
    [RequireOwner]
    public class AddModule : ModuleBase<DggCommandContext>
    {
        private readonly IConfiguration _config;
        private readonly TwitterService _twitterService;
        private readonly YoutubeService _youtubeService;

        public AddModule(IConfiguration config, TwitterService twitterService,YoutubeService youtubeService)
        {
            _config = config;
            _twitterService = twitterService;
            _youtubeService = youtubeService;
        }

        [Command("trusted")]
        public async Task TrustedUser(ulong userId)
        {
            using (var context = new DggContext())
            {
                var user = new TrustedUser
                {
                    UserId = userId,
                    Username = (await Context.Client.GetUserAsync(userId)).Username
                };
                context.TrustedUsers.Add(user);
                await context.SaveChangesAsync();
            }
        }

        [Command("twitter")]
        public async Task Twitter(string twitterName, long channelId)
        {
            
            var user = await _twitterService.GetUser(twitterName);
            if (user is null)
            {
                await ReplyAsync("Unable to get info from Twitter API");
                return;
            }
            using (var context = new DggContext())
            {
                if (await context.TwittersToCheck.FirstOrDefaultAsync(x => x.UserId == user.UserId) is null)
                {
                    user.DiscordChannelId = channelId;
                    user.DiscordServerId = (long)Context.Guild.Id;
                    user.Frequency = 10;
                    await context.TwittersToCheck.AddAsync(user);
                    var changes = await context.SaveChangesAsync();
                    if (changes > 0)
                    {
                        await ReplyAsync($"{user.FriendlyUsername} added to the database");
                        JobManager.AddJob(new TwitterJob(Context.SocketClient,user,_twitterService), (s) => s.ToRunEvery(user.Frequency).Seconds());
                        return;
                    }

                    await ReplyAsync($"Unable to save twitter to database");
                    return;

                }
                await ReplyAsync("Twitter account already exists in the Database");
            }
        }

        [Command("youtube")]
        public async Task Youtube(string twitterName, long channelId)
        {

            var user = await _twitterService.GetUser(twitterName);
            if (user is null)
            {
                await ReplyAsync("Unable to get info from Twitter API");
                return;
            }
            using (var context = new DggContext())
            {
                if (await context.TwittersToCheck.FirstOrDefaultAsync(x => x.UserId == user.UserId) is null)
                {
                    user.DiscordChannelId = channelId;
                    user.DiscordServerId = (long)Context.Guild.Id;
                    user.Frequency = 10;
                    await context.TwittersToCheck.AddAsync(user);
                    var changes = await context.SaveChangesAsync();
                    if (changes > 0)
                    {
                        await ReplyAsync($"{user.FriendlyUsername} added to the database");
                        JobManager.AddJob(new TwitterJob(Context.SocketClient, user, _twitterService), (s) => s.ToRunEvery(user.Frequency).Seconds());
                        return;
                    }

                    await ReplyAsync($"Unable to save twitter to database");
                    return;

                }
                await ReplyAsync("Twitter account already exists in the Database");
            }


        }


    }
}
