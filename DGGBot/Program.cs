using System.Diagnostics;
using System.Threading.Tasks;
using DGGBot.Services.Eval;
using DGGBot.Utilities;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SenpaiBot;

namespace DGGBot
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
           
            await new DggBot().Start();
        } 
    }
}