using System;
using System.Linq;
using System.Net;
using DGGBot.Data;
using DGGBot.Data.Enitities.Twitter;
using DGGBot.Models;
using DGGBot.Utilities;
using Discord;
using Discord.WebSocket;
using FluentScheduler;
using Serilog;

namespace DGGBot.Services.Twitter
{
    public class TwitterJob : IJob
    {
        private readonly DiscordSocketClient _client;
        private readonly TwitterToCheck _twitter;
        private readonly TwitterService _twitterService;

        public TwitterJob(DiscordSocketClient client, TwitterToCheck twitter, TwitterService twitterService)
        {
            _client = client;
            _twitter = twitter;
            _twitterService = twitterService;
        }

        public void Execute()
        {
            Log.Debug("Twitter check started for {twitter}", _twitter.FriendlyUsername);
            using (var context = new DggContext())
            {
                var existing = context.TweetRecords.FirstOrDefault(t => t.UserId == _twitter.UserId);
                var tweets = _twitterService.GetTweet(_twitter.UserId, existing?.TweetId).GetAwaiter().GetResult();
                var channel = _client.GetChannel((ulong) _twitter.DiscordChannelId) as SocketTextChannel;

                for (var i = 0; i < tweets.Count; i++)
                {
                    var tweet = tweets[i];

                    if (tweet.Text.StartsWith("@"))
                    {
                        tweets.RemoveAt(i);
                        i--;
                        continue;
                    }

                    var embed = CreateEmbed(tweet);

                    try
                    {
                        channel.SendMessageAsync("", embed: embed).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("[" + DateTime.UtcNow +
                                                "] TWITTER ERROR, CheckTwitterAsync");
                        Console.Error.WriteLine(ex.ToString());
                        Console.Error.WriteLine(ex.InnerException?.ToString());
                        Console.Error.WriteLine("------------");
                        Console.Error.WriteLine();
                    }

                    if (existing == null)
                    {
                        tweets.Reverse();
                        break;
                    }
                }
                if (tweets.Count == 0)
                    return;

                var latestTweet = tweets[tweets.Count - 1];

                using (var db = new DggContext())
                {
                    if (existing == null)
                    {
                        existing = new TweetRecord
                        {
                            UserId = latestTweet.User.Id,
                            TweetId = latestTweet.Id,
                            Text = WebUtility.HtmlDecode(latestTweet.Text),
                            AuthorName = latestTweet.User.Name,
                            AuthorUsername = latestTweet.User.Username,
                            ProfileImageUrl = latestTweet.User.ProfileImageUrl,
                            CreatedAt = latestTweet.CreatedAt.UtcDateTime
                        };

                        db.TweetRecords.Add(existing);
                    }
                    else
                    {
                        existing.TweetId = latestTweet.Id;
                        existing.Text = WebUtility.HtmlDecode(latestTweet.Text);
                        existing.AuthorName = latestTweet.User.Name;
                        existing.AuthorUsername = latestTweet.User.Username;
                        existing.ProfileImageUrl = latestTweet.User.ProfileImageUrl;
                        existing.CreatedAt = latestTweet.CreatedAt.UtcDateTime;

                        db.TweetRecords.Update(existing);
                    }

                    db.SaveChanges();
                }
            }
        }

        private Embed CreateEmbed(Tweet tweet)
        {
            var embed = new EmbedBuilder();

            var author = new EmbedAuthorBuilder
            {
                Name = $"{tweet.User.Username} ({tweet.User.Name})",
                Url = $"https://twitter.com/{tweet.User.Username}",
                IconUrl = tweet.User.ProfileImageUrl
            };

            var createdAt = TimeZoneInfo.ConvertTime(tweet.CreatedAt, Helpers.CentralTimeZone());

            var footer = new EmbedFooterBuilder
            {
                Text = $"Posted on {createdAt:MMM d, yyyy} at {createdAt:H:mm} Central"
            };

            embed.Title = "Go to tweet";
            embed.Description = WebUtility.HtmlDecode(tweet.Text);
            embed.Url = $"https://twitter.com/{tweet.User.Username}/status/{tweet.Id}";
            embed.Color = new Color((uint) _twitter.EmbedColor);
            embed.Author = author;
            embed.Footer = footer;

            return embed.Build();
        }
    }
}