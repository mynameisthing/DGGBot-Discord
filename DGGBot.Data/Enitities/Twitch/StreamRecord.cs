using System;

namespace DGGBot.Data.Enitities.Twitch
{
    public class StreamRecord
    {
        public long UserId { get; set; }

        public long StreamId { get; set; }

        public long DiscordMessageId { get; set; }

        public DateTime StartTime { get; set; }

        public string CurrentGame { get; set; }
        
    }

    public class StreamLastOnline
    {
        public long UserId { get; set; }

        public string FriendlyUsername { get; set; }

        public DateTime LastOnlineAt { get; set; }

        public string LastGame { get; set; }
    }
}