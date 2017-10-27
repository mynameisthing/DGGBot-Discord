using System.Threading.Tasks;

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