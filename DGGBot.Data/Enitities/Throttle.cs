using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGGBot.Data.Enitities
{
    public class Throttle
    {
        public int Id { get; set; }
        public ulong DiscordChannelId { get; set; }
        public string ModuleName { get; set; }
    }
}
