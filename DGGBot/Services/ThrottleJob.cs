using System;
using System.Linq;
using DGGBot.Data;
using DGGBot.Data.Enitities;
using FluentScheduler;
using Serilog;

namespace DGGBot.Services
{
    public class ThrottleJob : IJob
    {
        private readonly string _commandName;
        private readonly ulong _discordChannelId;

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
                    
                    context.Throttles.Add(new Throttle
                    {
                        DiscordChannelId = _discordChannelId,
                        CommandName = _commandName
                    });
                    context.SaveChanges();
                    JobManager.AddJob(new ThrottleJob(_commandName, _discordChannelId),
                        s => s.ToRunOnceIn(30).Seconds());
                }
                else
                {
                    context.Throttles.Remove(throttle);
                    context.SaveChanges();
                    Log.Information("Throttle ended in {channel} for {commandName}",_discordChannelId,_commandName);
                }
            }
        }
    }
}