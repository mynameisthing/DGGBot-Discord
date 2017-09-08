using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DGGBot.Data;
using DGGBot.Data.Enitities.Youtube;
using DGGBot.Models;
using Discord;
using Discord.WebSocket;
using FluentScheduler;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace DGGBot.Services.Youtube
{
    public class YoutubeJob : IJob
    {
        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly YoutubeService _youtubeService;
        private readonly YouTubeToCheck _youTubeToCheck;

        public YoutubeJob(
            DiscordSocketClient client,
            YoutubeService youtubeService,
            YouTubeToCheck youTubeToCheck,
            HttpClient httpClient,
            IConfiguration config
        )
        {
            _client = client;
            _youtubeService = youtubeService;
            _youTubeToCheck = youTubeToCheck;
            _httpClient = httpClient;
            _config = config;
        }

        public void Execute()
        {
            var videos = _youtubeService.GetYouTubeVideoListAsync(_youTubeToCheck.ChannelId, "").GetAwaiter()
                .GetResult();
            if (videos.Items is null)
                return;
            var latestVideo = videos.Items.FirstOrDefault();
            var latestVideoId = latestVideo.Id.VideoId;

            YouTubeRecord existingRecord;

            using (var db = new DggContext())
            {
                existingRecord = db.YouTubeRecords.FirstOrDefault(r => r.ChannelId == _youTubeToCheck.ChannelId);
            }


            if (existingRecord != null &&
                latestVideo.Snippet.PublishedAt.CompareTo(new DateTimeOffset(existingRecord.PublishedAt)) <= 0)
                return;
            //Console.WriteLine(latestVideo.Snippet.PublishedAt.CompareTo(new DateTimeOffset(existingRecord.PublishedAt)) <= 0);
            var channel = _client.GetChannel((ulong) _youTubeToCheck.DiscordChannelId) as SocketTextChannel;

            var embed = SendMessageAsync(_youTubeToCheck, latestVideo).GetAwaiter().GetResult();
            if (embed == null)
                return;

            using (var db = new DggContext())
            {
                if (existingRecord == null)
                {
                    existingRecord = new YouTubeRecord
                    {
                        ChannelId = _youTubeToCheck.ChannelId,
                        VideoId = latestVideoId,
                        VideoTitle = latestVideo.Snippet.Title,
                        VideoDescription = embed.Description,
                        ImageUrl = embed.Image.Value.Url,
                        AuthorName = embed.Author.Value.Name,
                        AuthorUrl = embed.Author.Value.Url,
                        AuthorIconUrl = embed.Author.Value.IconUrl,
                        PublishedAt = latestVideo.Snippet.PublishedAt.UtcDateTime
                    };

                    db.YouTubeRecords.Add(existingRecord);
                }
                else
                {
                    existingRecord.VideoId = latestVideoId;
                    existingRecord.VideoTitle = latestVideo.Snippet.Title;
                    existingRecord.VideoDescription = embed.Description;
                    existingRecord.ImageUrl = embed.Image.Value.Url;
                    existingRecord.AuthorName = embed.Author.Value.Name;
                    existingRecord.AuthorUrl = embed.Author.Value.Url;
                    existingRecord.AuthorIconUrl = embed.Author.Value.IconUrl;
                    existingRecord.PublishedAt = latestVideo.Snippet.PublishedAt.UtcDateTime;

                    db.YouTubeRecords.Update(existingRecord);
                }

                db.SaveChanges();
            }
        }

        private async Task<Embed> SendMessageAsync(YouTubeToCheck youTubeToCheck, YouTubeVideoListItem video)
        {
            try
            {
                var channel = _client.GetChannel((ulong) youTubeToCheck.DiscordChannelId) as SocketTextChannel;

                var embed = await CreateEmbedAsync(video);
                if (embed == null)
                {
                    await channel.SendMessageAsync(
                        "Something broke when posting a new YouTube video. Bug Thing about it. (error: e)");
                    return null;
                }

                await channel.SendMessageAsync($"{video.Snippet.ChannelTitle} posted a new YouTube video.",
                    embed: embed);

                return embed;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[" + DateTime.UtcNow + "] YOUTUBE ERROR, SendMessageAsync");
                Console.Error.WriteLine(ex.ToString());
                Console.Error.WriteLine(ex.InnerException?.ToString());
                Console.Error.WriteLine("------------");
                Console.Error.WriteLine();
            }

            return null;
        }

        private async Task<Embed> CreateEmbedAsync(YouTubeVideoListItem video)
        {
            video.Snippet.Thumbnails.Maxres =
                new YouTubeVideoThumbnail {Url = $"https://i.ytimg.com/vi/{video.Id.VideoId}/maxresdefault.jpg"};
            _httpClient.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");
            _httpClient.Timeout = new TimeSpan(0, 0, 8);
            // GET CHANNEL OBJECT FROM API
            var response = await _httpClient
                .GetAsync($"channels?part=snippet&id={video.Snippet.ChannelId}&key={_config["youtubeKey"]}");
            var responseString = await response.Content.ReadAsStringAsync();
            var channelList = JsonConvert.DeserializeObject<YouTubeChannelList>(responseString);
            if (channelList.Items.Count != 1)
                return null;

            var channel = channelList.Items[0];

            var embed = new EmbedBuilder();
            var author = new EmbedAuthorBuilder
            {
                Name = channel.Snippet.Title,
                Url = $"https://www.youtube.com/channel/{channel.Id}",
                IconUrl = channel.Snippet.Thumbnails.Default.Url
            };

            var publishedAt = TimeZoneInfo.ConvertTime(video.Snippet.PublishedAt, TimeZoneInfo.Local);

            var footer = new EmbedFooterBuilder
            {
                Text = $"Posted on {publishedAt:MMM d, yyyy} at {publishedAt:H:mm} Central"
            };

            var shortDescription = video.Snippet.Description;

            var lineBreakIndex = video.Snippet.Description.IndexOf("\n");
            if (lineBreakIndex != -1)
                shortDescription = shortDescription.Substring(0, lineBreakIndex);

            if (shortDescription.Length > 500)
                shortDescription = shortDescription.Substring(0, 500) + "...";

            embed.Author = author;
            embed.Footer = footer;
            embed.Color = new Color(205, 32, 31);
            embed.ImageUrl = video.Snippet.Thumbnails.Maxres.Url;
            embed.Title = video.Snippet.Title;
            embed.Description = shortDescription;
            embed.Url = $"https://www.youtube.com/watch?v={video.Id.VideoId}";

            return embed.Build();
        }
    }
}