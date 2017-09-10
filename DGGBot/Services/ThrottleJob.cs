using System;
using System.Linq;
using DGGBot.Data;
using DGGBot.Data.Enitities;
using FluentScheduler;

namespace DGGBot.Services
{
    public class ThrottleJob : IJob
    {
        private readonly ulong _discordChannelId;
        private readonly string _moduleName;

        public ThrottleJob(string moduleName, ulong discordChannelId)
        {
            _moduleName = moduleName;
            _discordChannelId = discordChannelId;
        }

        public void Execute()
        {
            using (var context = new DggContext())
            {
                var throttle = context.Throttles.FirstOrDefault(x =>
                    x.DiscordChannelId == _discordChannelId && x.ModuleName == _moduleName);
                if (throttle is null)
                {
                    Console.WriteLine("add throttle");
                    context.Throttles.Add(new Throttle
                    {
                        DiscordChannelId = _discordChannelId,
                        ModuleName = _moduleName
                    });
                    context.SaveChanges();
                    JobManager.AddJob(new ThrottleJob(_moduleName, _discordChannelId),
                        s => s.ToRunOnceIn(20).Seconds());
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