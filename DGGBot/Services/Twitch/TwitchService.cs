using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DGGBot.Models;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DGGBot.Services.Twitch
{
    public class TwitchService
    {
        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly string _url = "https://api.twitch.tv/kraken/streams/";

        public TwitchService(DiscordSocketClient client, IConfiguration config, HttpClient httpClient)
        {
            _client = client;
            _config = config;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_url);
            _httpClient.Timeout = new TimeSpan(0, 0, 8);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.twitchtv.v5+json"));
            _httpClient.DefaultRequestHeaders.Add("Client-ID", _config["twitchClientId"]);
        }

        public async Task<TwitchStream> GetTwitchStreamAsync(long userId)
        {
            var response = await _httpClient.GetAsync(userId.ToString());
            var responseString = await response.Content.ReadAsStringAsync();
            var streamResponse = JsonConvert.DeserializeObject<TwitchStreamResponse>(responseString, GetJsonSettings());
            //streamResponse.Stream = null;
            return streamResponse?.Stream;
        }

        public Embed CreateEmbed(TwitchStream stream, uint inColor)
        {
            var now = DateTime.UtcNow;
            var cacheBuster =
                now.Year +
                now.Month.ToString() +
                now.Day +
                now.Hour +
                now.Minute / 10 % 10;

            var embed = new EmbedBuilder();
            var color = new Color(inColor);
            var author = new EmbedAuthorBuilder();
            var imgUrl = stream.Preview.Template.Replace("{width}", "640").Replace("{height}", "360") + "?" +
                         cacheBuster;

            author.Name = stream.Channel.DisplayName ?? stream.Channel.Name;
            author.Url = stream.Channel.Url;
            author.IconUrl = stream.Channel.Logo;

            var streamPlayingField = new EmbedFieldBuilder
            {
                Name = "Playing",
                Value = !string.IsNullOrWhiteSpace(stream.Game) ? stream.Game : "(no game)",
                IsInline = true
            };

            var streamViewersField = new EmbedFieldBuilder
            {
                Name = "Viewers",
                Value = stream.Viewers.ToString(),
                IsInline = true
            };

            embed.Color = color;
            embed.ImageUrl = imgUrl;
            embed.Title = !string.IsNullOrWhiteSpace(stream.Channel.Status) ? stream.Channel.Status : "(no title)";
            embed.Url = stream.Channel.Url;
            embed.Author = author;

            embed.AddField(streamPlayingField);
            embed.AddField(streamViewersField);

            return embed.Build();
        }

        public static JsonSerializerSettings GetJsonSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy(true, false)
                },
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                Formatting = Formatting.Indented,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
        }
    }
}