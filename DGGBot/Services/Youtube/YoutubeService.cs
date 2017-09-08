using System;
using System.Net.Http;
using System.Threading.Tasks;
using DGGBot.Models;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DGGBot.Services.Youtube
{
    public class YoutubeService
    {
        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly string _url = "https://www.googleapis.com/youtube/v3/";

        public YoutubeService(DiscordSocketClient client, IConfiguration config, HttpClient httpClient)
        {
            _client = client;
            _config = config;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");
            _httpClient.Timeout = new TimeSpan(0, 0, 8);
        }

        public async Task<YouTubeVideoList> GetYouTubeVideoListAsync(string videoId)
        {
            var response = await _httpClient
                .GetAsync($"videos?part=snippet&id={videoId}&maxResults=10&key={_config["YoutubeKey"]}");
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<YouTubeVideoList>(responseString, GetJsonSettings());
        }

        public async Task<YouTubeVideoList> GetYouTubeVideoListAsync(string channelId, string test = "")
        {
            var response = await _httpClient
                .GetAsync(
                    $"search?part=snippet&&channelId={channelId}&maxResults=10&order=date&type=video&key={_config["YoutubeKey"]}");
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<YouTubeVideoList>(responseString, GetJsonSettings());
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