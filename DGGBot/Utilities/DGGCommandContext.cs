using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DGGBot.Utilities
{
    public class DggCommandContext : CommandContext
    {
        public DiscordSocketClient SocketClient { get; }

        public DggCommandContext(DiscordSocketClient socketClient, IDiscordClient client, IUserMessage msg) : base(client, msg)
        {
            SocketClient = socketClient;
        }
    }
}
