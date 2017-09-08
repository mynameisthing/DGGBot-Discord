using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DGGBot.Utilities;
using DGGBot.Utilities.Attributes;
using Discord.Commands;
using Newtonsoft.Json;

namespace DGGBot.Modules
{
    [Group("ferret")]
    [Alias("ferretlol", "<:FerretLOL:271856531857735680>")]
    [ChannelThrottle]
    public class FerretModule : ModuleBase<DggCommandContext>
    {
        private readonly HttpClient _client;


        public FerretModule(HttpClient client)
        {
            _client = client;
        }

        [Command]
        public async Task Ferret([Remainder] string unused = null)
        {
            var response = await _client.GetAsync("https://polecat.me/api/ferret");
            var responseString = await response.Content.ReadAsStringAsync();
            var ferret = JsonConvert.DeserializeObject<FerretResponse>(responseString);

            var img = await _client.GetAsync(ferret.Url);

            using (var stream = new MemoryStream())
            {
                await img.Content.CopyToAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, "ferret.png");
            }
        }

        private class FerretResponse
        {
            public string Url { get; set; }

            public long Last { get; set; }
        }
    }
}