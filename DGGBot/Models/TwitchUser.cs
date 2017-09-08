using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DGGBot.Models
{
    
    public class TwitchUser
    {
        [JsonProperty(PropertyName = "_id")]
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public class TwitchUserResponse
    {
        public List<TwitchUser> Users { get; set; }
    }
}
