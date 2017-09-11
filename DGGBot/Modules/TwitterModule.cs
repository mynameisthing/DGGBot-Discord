using System;
using System.Linq;
using System.Threading.Tasks;
using DGGBot.Data;
using DGGBot.Data.Enitities.Twitter;
using DGGBot.Services.Twitter;
using DGGBot.Utilities;
using DGGBot.Utilities.Attributes;
using Discord;
using Discord.Commands;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DGGBot.Modules
{
    [Group("twitter")]
    public class TwitterModule : ModuleBase<DggCommandContext>
    {
        private readonly TwitterService _twitterService;

        public TwitterModule(TwitterService twitterService)
        {
            _twitterService = twitterService;
        }

        [Command]
        [ChannelThrottle]
        public async Task GetTweet()
        {
            TweetRecord record;
            TwitterToCheck twitter;
            using (var db = new DggContext())
            {
                twitter = await db.TwittersToCheck.FirstOrDefaultAsync();
                record = await db.TweetRecords.FirstOrDefaultAsync(x => x.UserId == twitter.UserId);
            }

            if (record == null)
            {
                await ReplyAsync("Bot is on FIRE. Go Tell Thing");
                return;
            }

            record.CreatedAt = DateTime.SpecifyKind(record.CreatedAt, DateTimeKind.Utc);

            var embed = CreateEmbed(record, twitter);

            await ReplyAsync("", embed: embed);
        }

        [Command("add")]
        [RequireOwnerOrAdmin]
        public async Task AddTwitter(string twitterName, IGuildChannel guildChannel, string hexColor,
            int checkFrequency)
        {
            if (checkFrequency < 5)
            {
                await ReplyAsync("Frequency cant be less than 5");
            }
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
                    user.Frequency = checkFrequency;
                    user.EmbedColor = (int) Helpers.GetColorFromHex(hexColor).RawValue;
                    await context.TwittersToCheck.AddAsync(user);
                    var changes = await context.SaveChangesAsync();
                    if (changes > 0)
                    {
                        Log.Information("{author} added Twitter User:{user}", Context.Message.Author, twitterName);
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

        [Command("remove")]
        [RequireOwnerOrAdmin]
        public async Task RemoveTwitter(string twitterName)
        {
            using (var context = new DggContext())
            {
                var twitter =
                    await context.TwittersToCheck.FirstOrDefaultAsync(x =>
                        string.Compare(x.FriendlyUsername, twitterName, StringComparison.OrdinalIgnoreCase) == 0);
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
                    Log.Information("Job Count is {count}", JobManager.AllSchedules.Count());
                    JobManager.RemoveJob(twitter.UserId.ToString());
                    Log.Information("Job Count is {count}", JobManager.AllSchedules.Count());

                    await ReplyAsync("Twitter account removed from the Database");
                    return;
                }
                await ReplyAsync("Unable to remove twitter from the database");
            }
        }


        private Embed CreateEmbed(TweetRecord tweet, TwitterToCheck twitter)
        {
            var embed = new EmbedBuilder();

            var author = new EmbedAuthorBuilder
            {
                Name = tweet.AuthorUsername + " (" + tweet.AuthorName + ")",
                Url = "https://twitter.com/" + tweet.AuthorUsername,
                IconUrl = tweet.ProfileImageUrl
            };

            var createdAt = TimeZoneInfo.ConvertTime(tweet.CreatedAt, Helpers.CentralTimeZone());

            var footer = new EmbedFooterBuilder
            {
                Text = $"Posted on {createdAt:MMM d, yyyy} at {createdAt:H:mm} Central"
            };

            embed.Title = "Go to tweet";
            embed.Description = tweet.Text;
            embed.Url = $"https://twitter.com/{tweet.AuthorUsername}/status/{tweet.TweetId}";
            embed.Color = new Color((uint) twitter.EmbedColor);
            embed.Author = author;
            embed.Footer = footer;

            return embed.Build();
        }
    }
}