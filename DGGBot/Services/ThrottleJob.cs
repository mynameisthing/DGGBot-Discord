using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DGGBot.Data;
using DGGBot.Data.Enitities;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;

namespace DGGBot.Services
{
    public class ThrottleJob : IJob
    {
        private readonly string _moduleName;
        private readonly ulong _discordChannelId;

        public ThrottleJob(string moduleName,ulong discordChannelId)
        {
            _moduleName = moduleName;
            _discordChannelId = discordChannelId;
        }
        public void Execute()
        {
            using (var context = new DggContext())
            {
                var throttle =  context.Throttles.FirstOrDefault(x => x.DiscordChannelId == _discordChannelId && x.ModuleName == _moduleName);
                if (throttle is null)
                {
                    Console.WriteLine("add throttle");
                    context.Throttles.Add(new Throttle(){DiscordChannelId = _discordChannelId,ModuleName = _moduleName});
                    var stuff = context.SaveChanges();
                    JobManager.AddJob(new ThrottleJob(_moduleName, _discordChannelId), (s) => s.ToRunOnceIn(15).Seconds());

                }
                else
                {
                    Console.WriteLine("remove throttle");

                    context.Throttles.Remove(throttle);
                    context.SaveChanges();
                }
             

            }
          
        }
    }
}
