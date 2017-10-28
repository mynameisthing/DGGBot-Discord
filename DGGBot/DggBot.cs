using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using DGGBot.Data;
using DGGBot.Services.Twitch;
using DGGBot.Services.Twitter;
using DGGBot.Services.Youtube;
using DGGBot.Utilities;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;

namespace DGGBot
{
    public class DggBot
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IConfiguration _config;
        private readonly IServiceProvider _services;

        public DggBot()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.RollingFile(Path.Combine(Directory.GetCurrentDirectory(),"logs","{Date}.txt"))
                .CreateLogger();

            _services = BuildDependencies();
            _client = _services.GetRequiredService<DiscordSocketClient>();
            _commands = _services.GetRequiredService<CommandService>();
            _config = _services.GetRequiredService<IConfiguration>();
           
        }

        public async Task Start()
        {
            var token = _config["DiscordToken"];
            await InstallCommands();
            
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            _client.Ready += client_Ready;
           await Task.Delay(-1);
        }

       

        private async Task client_Ready()
        {
           

            
            CreateScheduledJobs(_services);
        }

        private async Task InstallCommands()
        {
            JobManager.JobException +=
                info => Log.Warning("An error just happened with a scheduled job: " + info.Exception);
            _client.Log += HandleLog;
            _commands.Log += HandleLog;
            _client.MessageReceived += HandleCommand;
            
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private async Task HandleCommand(SocketMessage messageParam)
        {
            if (!(messageParam is SocketUserMessage message))
            {
                return;
            }

            var argPos = 0;

            if (!(message.HasStringPrefix("! ", ref argPos)
                  || message.HasCharPrefix('!', ref argPos)
                  || message.HasMentionPrefix(_client.CurrentUser, ref argPos)))
            {
                return;
            }


            var context = new DggCommandContext(_client, message);

            await _commands.ExecuteAsync(context, argPos, _services);
        }

        //TRUMPED
        private static void CreateScheduledJobs(IServiceProvider serviceProvider)
        {
            try
            {
                var registry = new Registry();
                using (var context = new DggContext())
                {
                    context.Database.Migrate();
                    var throttles = context.Throttles;

                    context.Throttles.RemoveRange(throttles);
                    context.SaveChanges();

                    var twitters = context.TwittersToCheck;
                    foreach (var twitter in twitters)
                    {
                        registry.Schedule(new TwitterJob(
                                serviceProvider.GetRequiredService<DiscordSocketClient>(),
                                twitter,
                                serviceProvider.GetRequiredService<TwitterService>()))
                            .WithName(twitter.UserId.ToString())
                            .ToRunEvery(twitter.Frequency)
                            .Seconds();
                        Log.Information("{jobtype} started for {name} with ID: {id}", nameof(TwitterJob),
                            twitter.FriendlyUsername, twitter.UserId);
                    }


                    var youtubes = context.YouTubesToCheck;
                    foreach (var youTube in youtubes)
                    {
                        registry.Schedule(new YoutubeJob(
                                serviceProvider.GetRequiredService<DiscordSocketClient>(),
                                serviceProvider.GetRequiredService<YoutubeService>(),
                                youTube,
                                new HttpClient(),
                                serviceProvider.GetRequiredService<IConfiguration>())).WithName(youTube.ChannelId)
                            .WithName(youTube.ChannelId)
                            .ToRunEvery(youTube.Frequency)
                            .Seconds();
                        Log.Information("{jobtype} started for {name} with ID: {id}", nameof(YoutubeJob),
                            youTube.FriendlyUsername, youTube.ChannelId);
                    }

                    var streamsToCheck = context.StreamsToCheck.ToList();
                    foreach (var stream in streamsToCheck)
                    {
                        registry.Schedule(new TwitchJob(
                                serviceProvider.GetRequiredService<DiscordSocketClient>(),
                                serviceProvider.GetRequiredService<TwitchService>(),
                                stream))
                            .WithName(stream.UserId.ToString())
                            .ToRunEvery(30).Seconds();
                        Log.Information("{jobtype} started for {name} with ID: {id}", nameof(TwitchJob),
                            stream.FriendlyUsername, stream.UserId);
                    }


                    var streamRecords = context.StreamRecords;
                    foreach (var stream in streamRecords)
                    {
                        var thisStreamToCheck = streamsToCheck.Find(s => s.UserId == stream.UserId);
                        registry.Schedule(new TwitchUpdateJob(
                            serviceProvider.GetRequiredService<DiscordSocketClient>(),
                            serviceProvider.GetRequiredService<TwitchService>(),
                            thisStreamToCheck
                        )).WithName(stream.StreamId.ToString()).ToRunEvery(5).Minutes();
                        Log.Information("{jobtype} started for {name} with ID: {id}", nameof(TwitchUpdateJob),
                            thisStreamToCheck.FriendlyUsername, stream.StreamId);
                    }
                    JobManager.Initialize(registry);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static IServiceProvider BuildDependencies()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .Build();

            var discordConfig = new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            };
            var client = new DiscordSocketClient(discordConfig);
            var services = new ServiceCollection();
            var httpclient = new HttpClient();
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton(client);
            services.AddSingleton(httpclient);
            services.AddSingleton<InteractiveService>();
            services.AddSingleton(new CommandService());
            services.AddSingleton(new TwitterService(config));
            services.AddSingleton(new YoutubeService(config, new HttpClient()));
            services.AddSingleton(new TwitchService(config, new HttpClient()));

            return services.BuildServiceProvider();
        }

        private Task HandleLog(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    Log.Error(message.ToString());
                    break;
                case LogSeverity.Debug:
                    Log.Debug(message.ToString());
                    break;
                case LogSeverity.Warning:
                    Log.Warning(message.ToString());
                    break;
                case LogSeverity.Error:
                    Log.Error(message.ToString());
                    break;
                case LogSeverity.Info:
                    Log.Information(message.ToString());
                    break;
                case LogSeverity.Verbose:
                    Log.Verbose(message.ToString());
                    break;
            }
            return Task.CompletedTask;
        }
    }
}