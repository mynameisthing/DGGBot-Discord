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
using Discord.Commands;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DGGBot.Modules
{
    [Group("twitch")]
    [Alias("live")]
    public class TwitchModule : ModuleBase<DggCommandContext>
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

            using (var db = new DggContext())
            {
                lastOnline = await db.StreamLastOnlines.FirstOrDefaultAsync();
                streamRecord = await db.StreamRecords.FirstOrDefaultAsync();
            }

            string msg;

            if (lastOnline == null && streamRecord == null)
            {
                msg = $"Destiny is offline. " +
                      "I'll have more info in the future, after the next time the stream goes live. " +
                      "Sorry!";
            }
            else if (lastOnline == null && streamRecord != null)
            {
                streamRecord.StartTime = DateTime.SpecifyKind(streamRecord.StartTime, DateTimeKind.Utc);

                var duration = DateTime.UtcNow - streamRecord.StartTime;
                msg = $"Destiny is live! <https://www.destiny.gg/bigscreen>\n" +
                      $"Currently playing {streamRecord.CurrentGame}\n" +
                      $"Live for {duration.ToFriendlyString()}";
            }
            else
            {
                lastOnline.LastOnlineAt = DateTime.SpecifyKind(lastOnline.LastOnlineAt, DateTimeKind.Utc);

                var duration = DateTime.UtcNow - lastOnline.LastOnlineAt;
                msg = $"Destiny is offline.\n" +
                      $"Last online {duration.ToFriendlyString()} ago, was playing {lastOnline.LastGame}.";
            }

            await ReplyAsync(msg);
        }

        [Command("add")]
        [RequireOwnerOrAdmin]
        public async Task AddTwitch(string twitchName, IGuildChannel guildChannel, string hexColor, int checkFrequency,
            bool deleteMessage, bool pinMessage, [Remainder] string discordMessage)
        {
            if (checkFrequency < 5)
            {
                await ReplyAsync("Frequency cant be less than 5");
            }
            var response = await _twitchService.GetTwitchUserAsync(twitchName);
            if (response.Users is null)
            {
                await ReplyAsync("Unable to get info from Twitch API");
                return;
            }
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
                        Frequency = checkFrequency,
                        FriendlyUsername = user.Name,
                        DeleteDiscordMessage = deleteMessage,
                        PinMessage = pinMessage,
                        DiscordMessage = discordMessage,
                        EmbedColor = (int) Helpers.GetColorFromHex(hexColor).RawValue
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
                await ReplyAsync("Youtube account already exists in the Database");
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
    }
}