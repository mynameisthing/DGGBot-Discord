using System;

namespace DGGBot.Data.Enitities.Youtube
{
    public class YouTubeRecord
    {
        public string ChannelId { get; set; }

        public string VideoId { get; set; }

        public string VideoTitle { get; set; }

        public string VideoDescription { get; set; }

        public string AuthorName { get; set; }

        public string AuthorUrl { get; set; }

        public string AuthorIconUrl { get; set; }

        public string ImageUrl { get; set; }

        public DateTime PublishedAt { get; set; }
    }
}