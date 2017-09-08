using System;
using System.Net.Http;
using System.Threading.Tasks;
using DGGBot.Models;
using DGGBot.Utilities;
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
            _httpClient.BaseAddress = new Uri(_url);
            _httpClient.Timeout = new TimeSpan(0, 0, 8);
        }

        public async Task<YouTubeVideoList> GetYouTubeVideoListAsync(string channelId)
        {
            var response = await _httpClient
                .GetAsync(
                    $"search?part=snippet&&channelId={channelId}&maxResults=10&order=date&type=video&key={_config["YoutubeKey"]}");
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<YouTubeVideoList>(responseString, Helpers.GetJsonSettings());
        }
        public async Task<YouTubeChannelList> GetYouTubeVideoChannelInfoAsync(string channelName)
        {
            var response = await _httpClient
                .GetAsync(
                    $"channels?part=snippet&forUsername={channelName}&key={_config["YoutubeKey"]}");
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<YouTubeChannelList>(responseString, Helpers.GetJsonSettings());
        }


     
    }
}