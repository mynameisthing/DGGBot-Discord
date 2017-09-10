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
        private readonly string _commandName;

        public ThrottleJob(string commandName, ulong discordChannelId)
        {
            _commandName = commandName;
            _discordChannelId = discordChannelId;
        }

        public void Execute()
        {
            using (var context = new DggContext())
            {
                var throttle = context.Throttles.FirstOrDefault(x =>
                    x.DiscordChannelId == _discordChannelId && x.CommandName == _commandName);
                if (throttle is null)
                {
                    Console.WriteLine("add throttle");
                    context.Throttles.Add(new Throttle
                    {
                        DiscordChannelId = _discordChannelId,
                        CommandName = _commandName
                    });
                    context.SaveChanges();
                    JobManager.AddJob(new ThrottleJob(_commandName, _discordChannelId),
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