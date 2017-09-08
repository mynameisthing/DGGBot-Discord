using System;
using System.Threading.Tasks;
using DGGBot.Data;
using DGGBot.Data.Enitities.Twitter;
using DGGBot.Utilities;
using DGGBot.Utilities.Attributes;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;

namespace DGGBot.Modules
{
    public class TwitterModule : ModuleBase<DggCommandContext>
    {
        [Command("twitter")]
        [ChannelThrottle]
        public async Task GetTweet()
        {
            TweetRecord record;

            using (var db = new DggContext())
            {
                record = await db.TweetRecords.FirstOrDefaultAsync();
            }

            if (record == null)
            {
                await ReplyAsync("Bot is on FIRE. Go Tell Thing");
                return;
            }

            record.CreatedAt = DateTime.SpecifyKind(record.CreatedAt, DateTimeKind.Utc);

            var embed = CreateEmbed(record);

            await ReplyAsync("", embed: embed);
        }

        private Embed CreateEmbed(TweetRecord tweet)
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
            embed.Color = new Color(29, 161, 242);
            embed.Author = author;
            embed.Footer = footer;

            return embed.Build();
        }
    }
}