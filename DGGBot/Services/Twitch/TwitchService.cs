using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DGGBot.Models;
using DGGBot.Utilities;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace DGGBot.Services.Twitch
{
    public class TwitchService
    {
        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly string _streamUrl = "https://api.twitch.tv/kraken/streams/";
        private readonly string _userUrl = "https://api.twitch.tv/kraken/users";

        public TwitchService(DiscordSocketClient client, IConfiguration config, HttpClient httpClient)
        {
            _client = client;
            _config = config;
            _httpClient = httpClient;

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.twitchtv.v5+json"));
            _httpClient.DefaultRequestHeaders.Add("Client-ID", _config["TwitchClientId"]);
        }

        public async Task<TwitchStream> GetTwitchStreamAsync(long userId)
        {
            var response = await _httpClient.GetAsync($"{_streamUrl}{userId}");
            var responseString = await response.Content.ReadAsStringAsync();
            var streamResponse =
                JsonConvert.DeserializeObject<TwitchStreamResponse>(responseString, Helpers.GetJsonSettings());
            return streamResponse?.Stream;
        }

        public async Task<TwitchUserResponse> GetTwitchUserAsync(string userName)
        {
            var response = await _httpClient.GetAsync($"{_userUrl}?login={userName}");
            var responseString = await response.Content.ReadAsStringAsync();
            var streamResponse =
                JsonConvert.DeserializeObject<TwitchUserResponse>(responseString, Helpers.GetJsonSettings());
            return streamResponse;
        }
    }
}