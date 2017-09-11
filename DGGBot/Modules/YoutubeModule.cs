using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DGGBot.Data;
using DGGBot.Data.Enitities.Youtube;
using DGGBot.Services.Youtube;
using DGGBot.Utilities;
using DGGBot.Utilities.Attributes;
using Discord;
using Discord.Commands;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DGGBot.Modules
{
    [Group("youtube")]
    [Alias("yt")]
    public class YoutubeModule : ModuleBase<DggCommandContext>
    {
        private readonly IConfiguration _config;
        private readonly YoutubeService _youtubeService;

        public YoutubeModule(YoutubeService youtubeService, IConfiguration config)
        {
            _youtubeService = youtubeService;
            _config = config;
        }

        [Command]
        [ChannelThrottle]
        public async Task GetYouTube([Remainder] string unused = null)
        {
            YouTubeRecord record;
            YouTubeToCheck youtube;
            using (var db = new DggContext())
            {
                record = await db.YouTubeRecords.FirstOrDefaultAsync(y => y.ChannelId == "UC554eY5jNUfDq3yDOJYirOQ");
                youtube = await db.YouTubesToCheck.FirstOrDefaultAsync(y => y.ChannelId == "UC554eY5jNUfDq3yDOJYirOQ");
            }

            if (record == null)
            {
                await ReplyAsync("Bot is on FIRE. Go Tell Thing");
                return;
            }

            record.PublishedAt = DateTime.SpecifyKind(record.PublishedAt, DateTimeKind.Utc);

            var embed = CreateEmbed(record, youtube);

            await ReplyAsync("", embed: embed);
        }

        [Command("add")]
        public async Task Youtube(string youtubeName, IGuildChannel guildChannel, string hexColor, int checkFrequency)
        {
            if (checkFrequency < 5)
            {
                await ReplyAsync("Frequency cant be less than 5");
            }
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
                        FriendlyUsername = channel.Snippet.Title,
                        EmbedColor = (int) Helpers.GetColorFromHex(hexColor).RawValue
                    };

                    await context.YouTubesToCheck.AddAsync(youtubeToCheck);
                    var changes = await context.SaveChangesAsync();
                    if (changes > 0)
                    {
                        Log.Information("{author} added Youtube Channel:{channel}", Context.Message.Author,
                            youtubeName);
                        await ReplyAsync($"{channel.Snippet.Title} added to the database");
                        JobManager.AddJob(
                            new YoutubeJob(Context.SocketClient, _youtubeService, youtubeToCheck, new HttpClient(),
                                _config),
                            s => s.WithName(youtubeToCheck.ChannelId).ToRunEvery(youtubeToCheck.Frequency)
                                .Seconds());
                        return;
                    }

                    await ReplyAsync($"Unable to save youtube to database");
                    return;
                }
                await ReplyAsync("Youtube account already exists in the Database");
            }
        }

        [Command("remove")]
        public async Task Youtube(string youtubeName)
        {
            using (var context = new DggContext())
            {
                var youtube =
                    await context.YouTubesToCheck.FirstOrDefaultAsync(x =>
                        string.Compare(x.FriendlyUsername, youtubeName, StringComparison.OrdinalIgnoreCase) == 0);
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
                    Log.Information("{author} removed Youtube channel:{channel}", Context.Message.Author,
                        youtubeName);

                    Log.Information("Job Count is {count}", JobManager.AllSchedules.Count());
                    JobManager.RemoveJob(youtube.ChannelId);
                    Log.Information("Job Count is {count}", JobManager.AllSchedules.Count());

                    await ReplyAsync("Youtube account removed from the Database");
                    return;
                }
                await ReplyAsync("Unable to remove Youtube from the database");
            }
        }

        private Embed CreateEmbed(YouTubeRecord record, YouTubeToCheck youtube)
        {
            var embed = new EmbedBuilder();
            var author = new EmbedAuthorBuilder
            {
                Name = record.AuthorName,
                Url = record.AuthorUrl,
                IconUrl = record.AuthorIconUrl
            };

            var publishedAt = TimeZoneInfo.ConvertTime(record.PublishedAt, Helpers.CentralTimeZone());

            var footer = new EmbedFooterBuilder
            {
                Text = $"Posted on {publishedAt:MMM d, yyyy} at {publishedAt:H:mm} Central"
            };

            var descriptionField = new EmbedFieldBuilder
            {
                Name = "Description",
                Value = record.VideoDescription,
                IsInline = false
            };

            embed.Author = author;
            embed.Footer = footer;
            embed.Color = new Color((uint) youtube.EmbedColor);
            embed.ImageUrl = record.ImageUrl;
            embed.Title = record.VideoTitle;
            embed.Url = "https://www.youtube.com/watch?v=" + record.VideoId;

            embed.AddField(descriptionField);

            return embed.Build();
        }
    }
}