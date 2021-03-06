﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace DGGBot.Models
{
    public class TwitchUser
    {
        [JsonProperty(PropertyName = "_id")]
        public long Id { get; set; }

        [JsonProperty(PropertyName = "display_name")]
        public string Name { get; set; }
    }

    public class TwitchUserResponse
    {
        public List<TwitchUser> Users { get; set; }
    }
}