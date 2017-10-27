using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DGGBot.Utilities
{
    public class DggCommandContext : SocketCommandContext
    {
        public DggCommandContext(DiscordSocketClient client, SocketUserMessage msg) : base(client, msg)
        {
            SocketClient = client;
        }

        public DiscordSocketClient SocketClient { get; }
    }
}