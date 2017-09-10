using System;
using System.Threading.Tasks;
using DGGBot.Data;
using DGGBot.Data.Enitities.Youtube;
using DGGBot.Utilities;
using DGGBot.Utilities.Attributes;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;

namespace DGGBot.Modules
{
    public class YoutubeModule : ModuleBase<DggCommandContext>
    {
        [Command("youtube")]
        [Alias("yt")]
        [ChannelThrottle]
        public async Task GetYouTube([Remainder] string unused = null)
        {
            YouTubeRecord record;

            using (var db = new DggContext())
            {
                record = await db.YouTubeRecords.FirstOrDefaultAsync(y => y.ChannelId == "UC554eY5jNUfDq3yDOJYirOQ");
            }

            if (record == null)
            {
                await ReplyAsync("Bot is on FIRE. Go Tell Thing");
                return;
            }

            record.PublishedAt = DateTime.SpecifyKind(record.PublishedAt, DateTimeKind.Utc);

            var embed = CreateEmbed(record);

            await ReplyAsync("", embed: embed);
        }

        private Embed CreateEmbed(YouTubeRecord record)
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
            embed.Color = new Color(205, 32, 31);
            embed.ImageUrl = record.ImageUrl;
            embed.Title = record.VideoTitle;
            embed.Url = "https://www.youtube.com/watch?v=" + record.VideoId;

            embed.AddField(descriptionField);

            return embed.Build();
        }
    }
}