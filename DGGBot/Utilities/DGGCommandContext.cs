using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DGGBot.Utilities
{
    public class DggCommandContext : CommandContext
    {
        public DggCommandContext(DiscordSocketClient client, IUserMessage msg) : base(client, msg)
        {
            SocketClient = client;
            
        }
      
        public DiscordSocketClient SocketClient { get; }
    }
}