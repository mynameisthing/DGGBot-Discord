using System;
using System.Linq;
using System.Threading.Tasks;
using DGGBot.Data;
using DGGBot.Data.Enitities.Twitch;
using DGGBot.Extensions;
using DGGBot.Services.Twitch;
using DGGBot.Utilities;
using DGGBot.Utilities.Attributes;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DGGBot.Modules
{
    [Group("twitch")]
    [Alias("live")]
    public class TwitchModule : InteractiveBase<DggCommandContext>
    {
        private readonly TwitchService _twitchService;

        public TwitchModule(TwitchService twitchService)
        {
            _twitchService = twitchService;
        }

        [Command]
        [ChannelThrottle]
        public async Task GetLive(string unused = null)
        {
            StreamLastOnline lastOnline;
            StreamRecord streamRecord;
            StreamToCheck streamToCheck;
            using (var db = new DggContext())
            {
                streamToCheck =  (await db.StreamsToCheck.ToListAsync()).Aggregate((firstStream, secondStream) => firstStream.Priority > secondStream.Priority ? firstStream : secondStream);
                lastOnline = await db.StreamLastOnlines.FirstOrDefaultAsync(x => x.UserId == streamToCheck.UserId);
                streamRecord = await db.StreamRecords.FirstOrDefaultAsync(x => x.UserId == streamToCheck.UserId);
            }
            
            string msg;

            if (lastOnline == null && streamRecord == null)
            {
                msg = $"{streamToCheck.FriendlyUsername} is offline. " +
                      "I'll have more info in the future, after the next time the stream goes live. " +
                      "Sorry!";
            }
            else if (lastOnline == null && streamRecord != null)
            {
                streamRecord.StartTime = DateTime.SpecifyKind(streamRecord.StartTime, DateTimeKind.Utc);

                var duration = DateTime.UtcNow - streamRecord.StartTime;
                msg = $"{streamToCheck.FriendlyUsername} is live! <{streamToCheck.StreamUrl}>\n" +
                      $"Currently playing {streamRecord.CurrentGame}\n" +
                      $"Live for {duration.ToFriendlyString()}";
            }
            else
            {
                lastOnline.LastOnlineAt = DateTime.SpecifyKind(lastOnline.LastOnlineAt, DateTimeKind.Utc);

                var duration = DateTime.UtcNow - lastOnline.LastOnlineAt;
                msg = $"{streamToCheck.FriendlyUsername} is offline.\n" +
                      $"Last online {duration.ToFriendlyString()} ago, was playing {lastOnline.LastGame}.";
            }

            await ReplyAsync(msg);
        }

        [Command("add",RunMode = RunMode.Async)]
        [RequireOwnerOrAdmin]
        
        public async Task AddTwitch(string twitchName, IGuildChannel guildChannel)
        {
            
            var response = await _twitchService.GetTwitchUserAsync(twitchName);
            if (response.Users is null)
            {
                await ReplyAsync("Unable to get Streamer from Twitch API");
                return;
            }
            await ReplyAsync("Please Enter in the Embed color in Hex format e.g. #ff851b\n" +
                             "You pick a color and get the code for here: <http://htmlcolorcodes.com>\n" +
                             "type in default to use the default color");
            var hexMessage = await NextMessageAsync(timeout:TimeSpan.FromSeconds(30));
            int hexColor;
            if (hexMessage.Content.Equals("default",StringComparison.OrdinalIgnoreCase))
            {
                hexColor = (int)Helpers.GetColorFromHex("#010aad").RawValue; 
            }
            else
            {
                hexColor = (int)Helpers.GetColorFromHex(hexMessage.Content).RawValue;

            }

            await ReplyAsync("Please Enter a message you would like to go along with the twitch embed. Type in default for no message" );
            var message = await NextMessageAsync(timeout: TimeSpan.FromSeconds(30));
            
           
            await ReplyAsync("Please Enter the stream URL. type in default to use twitch url");
            var urlMessage = await NextMessageAsync(timeout: TimeSpan.FromSeconds(30));

            await ReplyAsync("Please Enter a number for the priority");
            var priorityMessage = await NextMessageAsync(timeout: TimeSpan.FromSeconds(30));
            using (var context = new DggContext())
            {
                var user = response.Users.FirstOrDefault();
                if (await context.StreamsToCheck.FirstOrDefaultAsync(x => x.UserId == user.Id) is null)
                {
                    var streamToCheck = new StreamToCheck
                    {
                        DiscordChannelId = (long) guildChannel.Id,
                        DiscordServerId = (long) Context.Guild.Id,
                        UserId = user.Id,
                        Frequency = 60,
                        FriendlyUsername = user.Name,
                        DeleteDiscordMessage = false,
                        PinMessage = true,
                        Priority = Convert.ToInt32(priorityMessage.Content),
                        StreamUrl = urlMessage.Content.Equals("default", StringComparison.OrdinalIgnoreCase) ? $"https://twitch.tv/{user.Name}" : message.Content,
                        DiscordMessage = message.Content.Equals("default", StringComparison.OrdinalIgnoreCase) ? String.Empty : message.Content,
                        EmbedColor = hexColor
                    };

                    await context.StreamsToCheck.AddAsync(streamToCheck);
                    var changes = await context.SaveChangesAsync();
                    if (changes > 0)
                    {
                        Log.Information("{author} added Twitch Channel:{channel}", Context.Message.Author,
                            twitchName);
                        await ReplyAsync($"{user.Name} added to the database");
                        JobManager.AddJob(new TwitchJob(Context.SocketClient, _twitchService, streamToCheck),
                            s => s.WithName(streamToCheck.UserId.ToString()).ToRunEvery(streamToCheck.Frequency)
                                .Seconds());
                        return;
                    }

                    await ReplyAsync($"Unable to save youtube to database");
                    return;
                }
                await ReplyAsync("Twitch account already exists in the Database");
            }
        }

        [Command("remove")]
        [RequireOwnerOrAdmin]
        public async Task RemoveTwitch(string twitchName)
        {
            using (var context = new DggContext())
            {
                var stream = await context.StreamsToCheck.FirstOrDefaultAsync(x =>
                    string.Compare(x.FriendlyUsername, twitchName, StringComparison.OrdinalIgnoreCase) == 0);
                if (stream is null)
                {
                    await ReplyAsync("Twitch account not found in Database");
                    return;
                }

                var records = context.StreamRecords.Where(x => x.UserId == stream.UserId);
                var record = records.FirstOrDefault();
                var games = context.StreamGames.Where(x => x.StreamId == records.FirstOrDefault().StreamId);
                var responses = context.StreamNullResponses.Where(x => x.UserId == stream.UserId);

                context.StreamsToCheck.Remove(stream);
                context.StreamRecords.RemoveRange(records);
                context.StreamGames.RemoveRange(games);
                context.StreamNullResponses.RemoveRange(responses);

                var changes = await context.SaveChangesAsync();
                if (changes > 0)
                {
                    Log.Information("{author} removed Twitch channel:{channel}", Context.Message.Author,
                        twitchName);

                    Log.Information("Job Count is {count}", JobManager.AllSchedules.Count());
                    JobManager.RemoveJob(stream.UserId.ToString());
                    JobManager.RemoveJob(record?.StreamId.ToString());
                    Log.Information("Job Count is {count}", JobManager.AllSchedules.Count());

                    await ReplyAsync("Twitch account removed from the Database");
                    return;
                }
                await ReplyAsync("Unable to remove Twitch from the database");
            }
        }
        [Command("priority")]
        public async Task ChangePriority(string twitchName,int priority)
        {
            using (var context = new DggContext())
            {
                var stream = await context.StreamsToCheck.FirstOrDefaultAsync(x => x.FriendlyUsername.Equals(twitchName,StringComparison.OrdinalIgnoreCase));
                if (stream is null)
                {
                    await ReplyAsync("Twitch account not found in Database");
                    return;
                }

                stream.Priority = priority;
                var changes = await context.SaveChangesAsync();
                if (changes > 0)
                {
                    await ReplyAsync($"{twitchName} priority changed to {priority}");
                    return;
                }

                await ReplyAsync($"Unable to change priority");
            }
        }
    }
}