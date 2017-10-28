using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DGGBot.Utilities;
using DGGBot.Utilities.Attributes;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace DGGBot.Modules
{
    [Group("cat")]
    [Alias("aslan")]
    [ChannelThrottle]
    public class CatModule : ModuleBase<DggCommandContext>
    {
        private readonly HttpClient _client;
        private readonly IConfiguration _config;

        private readonly Random _rng = new Random();

        public CatModule(HttpClient client, IConfiguration config)
        {
            _client = client;
            _config = config;
        }

        [Command(RunMode = RunMode.Async)]
        [Summary("Gets a picture of Aslan")]
        public async Task Cat([Remainder] string unused = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.imgur.com/3/album/ohOjC/images");
            request.Headers.Authorization = new AuthenticationHeaderValue("Client-ID", _config["imgurClientId"]);

            var response = await _client.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();
            var album = JsonConvert.DeserializeObject<ImgurAlbum>(responseString);
            var imageList = album.Data;

            if (imageList == null || imageList.Count == 0)
            {
                await ReplyAsync("Error getting an Aslan picture. Probably not my fault. Sorry!");
                return;
            }

            var image = imageList[_rng.Next(imageList.Count)];
            var imgUri = new Uri(image.Link.Replace("http://", "https://"));

            var img = await _client.GetAsync(imgUri);

            using (var stream = new MemoryStream())
            {
                await img.Content.CopyToAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, Path.GetFileName(imgUri.AbsolutePath));
            }
        }

        private class ImgurAlbum
        {
            public List<ImgurImage> Data { get; set; }
        }

        private class ImgurImage
        {
            public string Id { get; set; }

            public string Link { get; set; }
        }
    }
}