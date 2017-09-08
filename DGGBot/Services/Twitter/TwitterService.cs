using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DGGBot.Data.Enitities.Twitter;
using DGGBot.Models;
using DGGBot.Utilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;

namespace DGGBot.Services.Twitter
{
    public class TwitterService
    {
        private readonly IConfiguration _config;


        public TwitterService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<List<Tweet>> GetTweet(long userId, long? sinceId)
        {
            try
            {
                var header = new OAuthHeader(_config["twitterKey"], _config["twitterKeySecret"], _config["twitterToken"],
                    _config["twitterTokenSecret"]);
                var url = $"https://api.twitter.com/1.1/statuses/user_timeline.json?user_id={userId}&count=50";
                if (sinceId.HasValue)
                    url += $"&since_id={sinceId.Value}";
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = header.Generate(url, "GET");
                    var response = await client.GetAsync(url);
                    var responseString = await response.Content.ReadAsStringAsync();
                    var tweets = JsonConvert.DeserializeObject<List<Tweet>>(responseString);

                    if (sinceId.HasValue)
                        tweets.Reverse();

                    return tweets;
                }
            }
            catch (Exception e)
            {
                Log.Debug(e,"Get Tweet");
                return new List<Tweet>();
               
            }
          
        }
        public async Task<TwitterToCheck> GetUser(string handle)
        {
            try
            {
                var header = new OAuthHeader(_config["twitterKey"], _config["twitterKeySecret"], _config["twitterToken"],_config["twitterTokenSecret"]);
                var url = $"https://api.twitter.com/1.1/users/show.json?screen_name={handle}";

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = header.Generate(url, "GET");
                    var response = await client.GetAsync(url);
                    var responseString = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<TwitterToCheck>(responseString);

                   
                }

            }
            catch (Exception e)
            {
                Log.Debug(e, "Get Tweet");
                return new TwitterToCheck();

            }

        }
    }
}