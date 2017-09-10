using Newtonsoft.Json;

namespace DGGBot.Data.Enitities.Twitter
{
    public class TwitterToCheck
    {
        [JsonProperty("id")]
        public long UserId { get; set; }
        [JsonProperty("screen_name")]
        public string FriendlyUsername { get; set; }

        public int Frequency { get; set; }

        public long DiscordServerId { get; set; }

        public long DiscordChannelId { get; set; }
        public int EmbedColor { get; set; }
    }
}