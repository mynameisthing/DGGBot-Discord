using System;
using System.Drawing;
using System.Threading.Tasks;
using DGGBot.Utilities;
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