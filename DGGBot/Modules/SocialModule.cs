using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DGGBot.Data;
using DGGBot.Data.Enitities.Twitch;
using DGGBot.Data.Enitities.Youtube;
using DGGBot.Services.Twitch;
using DGGBot.Services.Twitter;
using DGGBot.Services.Youtube;
using DGGBot.Utilities;
using Discord;
using Discord.Commands;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DGGBot.Modules
{
    [Group("social")]
    [RequireOwner]
    public class SocialModule : ModuleBase<DggCommandContext>
    {
        [Group("add")]
        public class AddModule : ModuleBase<DggCommandContext>
        {
            private readonly IConfiguration _config;
            private readonly TwitchService _twitchService;
            private readonly TwitterService _twitterService;
            private readonly YoutubeService _youtubeService;

            public AddModule(IConfiguration config, TwitterService twitterService, YoutubeService youtubeService,
                TwitchService twitchService)
            {
                _config = config;
                _twitterService = twitterService;
                _youtubeService = youtubeService;
                _twitchService = twitchService;
            }

            [Command("twitter")]
            public async Task Twitter(string twitterName, IGuildChannel guildChannel)
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
                        user.DiscordChannelId = (long) guildChannel.Id;
                        user.DiscordServerId = (long) Context.Guild.Id;
                        user.Frequency = 10;
                        await context.TwittersToCheck.AddAsync(user);
                        var changes = await context.SaveChangesAsync();
                        if (changes > 0)
                        {
                            Log.Information("{author} added Twitter User:{user}",Context.Message.Author,twitterName);
                            await ReplyAsync($"{user.FriendlyUsername} added to the database");
                            JobManager.AddJob(new TwitterJob(Context.SocketClient, user, _twitterService),
                                s => s.WithName(user.UserId.ToString()).ToRunEvery(user.Frequency).Seconds());
                            return;
                        }

                        await ReplyAsync($"Unable to save twitter to database");
                        return;
                    }
                    await ReplyAsync("Twitter account already exists in the Database");
                }
            }

            [Command("youtube")]
            public async Task Youtube(string youtubeName, IGuildChannel guildChannel)
            {
                var channels = await _youtubeService.GetYouTubeVideoChannelInfoAsync(youtubeName);
                if (channels.Items is null)
                {
                    await ReplyAsync("Unable to get info from Youtube API");
                    return;
                }
                using (var context = new DggContext())
                {
                    var channel = channels.Items.FirstOrDefault();
                    if (await context.YouTubesToCheck.FirstOrDefaultAsync(x => x.ChannelId == channel.Id) is null)
                    {
                        var youtubeToCheck = new YouTubeToCheck
                        {
                            DiscordChannelId = (long) guildChannel.Id,
                            DiscordServerId = (long) Context.Guild.Id,
                            ChannelId = channel.Id,
                            Frequency = 60,
                            FriendlyUsername = channel.Snippet.Title
                        };

                        await context.YouTubesToCheck.AddAsync(youtubeToCheck);
                        var changes = await context.SaveChangesAsync();
                        if (changes > 0)
                        {
                            Log.Information("{author} added Youtube Channel:{channel}", Context.Message.Author, youtubeName);
                            await ReplyAsync($"{channel.Snippet.Title} added to the database");
                            JobManager.AddJob(
                                new YoutubeJob(Context.SocketClient, _youtubeService, youtubeToCheck, new HttpClient(),
                                    _config), s => s.WithName(youtubeToCheck.ChannelId).ToRunEvery(youtubeToCheck.Frequency).Seconds());
                            return;
                        }

                        await ReplyAsync($"Unable to save youtube to database");
                        return;
                    }
                    await ReplyAsync("Youtube account already exists in the Database");
                }
            }

            [Command("twitch")]
            public async Task Twitch(string twitchName, IGuildChannel guildChannel, bool deleteMessage, bool pinMessage)
            {
                var response = await _twitchService.GetTwitchUserAsync(twitchName);
                if (response.Users is null)
                {
                    await ReplyAsync("Unable to get info from Twitch API");
                    return;
                }
                using (var context = new DggContext())
                {
                    var user = response.Users.FirstOrDefault();
                    if (await context.StreamsToCheck.FirstOrDefaultAsync(x => x.UserId == user.Id) is null)
                    {
                        var streamToCheck = new StreamToCheck
                        {
                            DiscordChannelId = (long) guildChannel.Id,
                            DiscordServerId = (long) Context.Guild.Id,
                            UserId = user.Id,
                            Frequency = 150,
                            FriendlyUsername = user.Name,
                            DeleteDiscordMessage = deleteMessage,
                            PinMessage = pinMessage,
                            DiscordMessage = "Memes",
                            EmbedColor = 29913
                        };

                        await context.StreamsToCheck.AddAsync(streamToCheck);
                        var changes = await context.SaveChangesAsync();
                        if (changes > 0)
                        {
                            Log.Information("{author} added Twitch Channel:{channel}", Context.Message.Author, twitchName);
                            await ReplyAsync($"{user.Name} added to the database");
                            JobManager.AddJob(new TwitchJob(Context.SocketClient, _twitchService, streamToCheck),
                                s => s.WithName(streamToCheck.UserId.ToString()).ToRunEvery(streamToCheck.Frequency).Seconds());
                            return;
                        }

                        await ReplyAsync($"Unable to save youtube to database");
                        return;
                    }
                    await ReplyAsync("Youtube account already exists in the Database");
                }
            }
        }
        [Group("remove")]
        public class RemoveModule : ModuleBase<DggCommandContext>
        {
            private readonly IConfiguration _config;
            private readonly TwitchService _twitchService;
            
            private readonly YoutubeService _youtubeService;

            public RemoveModule(IConfiguration config,YoutubeService youtubeService)
            {
                _config = config;
               
                _youtubeService = youtubeService;
                
            }

            [Command("twitter")]
            public async Task Twitter(string twitterName)
            {
                using (var context = new DggContext())
                {
                    var twitter =
                        await context.TwittersToCheck.FirstOrDefaultAsync(x => string.Compare(x.FriendlyUsername, twitterName,StringComparison.OrdinalIgnoreCase) == 0);
                    if (twitter is null)
                    {
                        await ReplyAsync("Twitter account not found in Database");
                        return;
                    }
                    var records = context.TweetRecords.Where(x => x.UserId == twitter.UserId);
                    context.TwittersToCheck.Remove(twitter);
                    context.TweetRecords.RemoveRange(records);

                    var changes = await context.SaveChangesAsync();
                    if (changes > 0)
                    {
                        Log.Information("{author} removed Twitter User:{user}", Context.Message.Author, twitterName);
                        Log.Information("Job Count is {count}",JobManager.AllSchedules.Count());
                        JobManager.RemoveJob(twitter.UserId.ToString());
                        Log.Information("Job Count is {count}", JobManager.AllSchedules.Count());

                        await ReplyAsync("Twitter account removed from the Database");
                        return;
                    }
                    await ReplyAsync("Unable to remove twitter from the database");
                }
            }

            [Command("youtube")]
            public async Task Youtube(string youtubeName)
            {
                using (var context = new DggContext())
                {
                    var youtube =
                        await context.YouTubesToCheck.FirstOrDefaultAsync(x => string.Compare(x.FriendlyUsername, youtubeName, StringComparison.OrdinalIgnoreCase) == 0);
                    if (youtube is null)
                    {
                        await ReplyAsync("Youtube account not found in Database");
                        return;
                    }
                    var records = context.YouTubeRecords.Where(x => x.ChannelId == youtube.ChannelId);
                    context.YouTubesToCheck.Remove(youtube);
                    context.YouTubeRecords.RemoveRange(records);

                    var changes = await context.SaveChangesAsync();
                    if (changes > 0)
                    {
                        Log.Information("{author} removed Youtube channel:{channel}", Context.Message.Author, youtubeName);

                        Log.Information("Job Count is {count}", JobManager.AllSchedules.Count());
                        JobManager.RemoveJob(youtube.ChannelId);
                        Log.Information("Job Count is {count}", JobManager.AllSchedules.Count());

                        await ReplyAsync("Youtube account removed from the Database");
                        return;
                    }
                    await ReplyAsync("Unable to remove Youtube from the database");
                }
            }

            [Command("twitch")]
            public async Task Twitch(string twitchName)
            {
                using (var context = new DggContext())
                {

                    var stream = await context.StreamsToCheck.FirstOrDefaultAsync(x => string.Compare(x.FriendlyUsername, twitchName, StringComparison.OrdinalIgnoreCase) == 0);
                    if (stream is null)
                    {
                        await ReplyAsync("Twitch account not found in Database");
                        return;
                    }

                    var records = context.StreamRecords.Where(x => x.UserId == stream.UserId);
                    var record = records.FirstOrDefault();
                    var games = context.StreamGames.Where(x => x.StreamId == records.FirstOrDefault().StreamId);
                    var responses = context.StreamNullResponses.Where(x => x.UserId == stream.UserId);

                    context.StreamsToCheck.Remove(stream);
                    context.StreamRecords.RemoveRange(records);
                    context.StreamGames.RemoveRange(games);
                    context.StreamNullResponses.RemoveRange(responses);

                    var changes = await context.SaveChangesAsync();
                    if (changes > 0)
                    {
                        Log.Information("{author} removed Twitch channel:{channel}", Context.Message.Author, twitchName);

                        Log.Information("Job Count is {count}", JobManager.AllSchedules.Count());
                        JobManager.RemoveJob(stream.UserId.ToString());
                        JobManager.RemoveJob(record?.StreamId.ToString());
                        Log.Information("Job Count is {count}", JobManager.AllSchedules.Count());

                        await ReplyAsync("Twitch account removed from the Database");
                        return;
                    }
                    await ReplyAsync("Unable to remove Twitch from the database");
                }
            }
            }
        }
    }
