using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DGGBot.Data;
using DGGBot.Data.Enitities.Twitch;
using DGGBot.Extensions;
using DGGBot.Models;
using DGGBot.Utilities;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DGGBot.Services.Twitch
{
    public class TwitchJob : IJob
    {
        private readonly DiscordSocketClient _client;
        private readonly StreamToCheck _stream;
        private readonly TwitchService _twitchService;

        public TwitchJob(DiscordSocketClient client, TwitchService twitchService, StreamToCheck stream)
        {
            _client = client;
            _stream = stream;
            _twitchService = twitchService;
        }


        public void Execute()
        {
            Log.Debug("Stream check started for {stream}", _stream.FriendlyUsername);
            var streamToCheck = _stream;

            var stream = _twitchService.GetTwitchStreamAsync(streamToCheck.UserId).GetAwaiter().GetResult();

            StreamRecord existingRecord;

            using (var db = new DggContext())
            {
                existingRecord = db.StreamRecords.FirstOrDefault(sr => sr.UserId == streamToCheck.UserId);
            }

            // live and was not previously live
            if (stream != null && existingRecord == null)
            {
                var msgId = SendMessageAsync(streamToCheck, stream).GetAwaiter().GetResult();

                using (var db = new DggContext())
                {
                    db.StreamRecords.Add(new StreamRecord
                    {
                        UserId = stream.Channel.Id,
                        StreamId = stream.Id,
                        DiscordMessageId = msgId,
                        CurrentGame = stream.Game,
                        StartTime = DateTime.UtcNow
                    });

                    db.SaveChanges();

                    db.StreamGames.Add(new StreamGame
                    {
                        Game = stream.Game,
                        StreamId = stream.Id,
                        StartTime = DateTime.UtcNow
                    });

                    var lastOnlines = db.StreamLastOnlines.Where(s => s.UserId == streamToCheck.UserId);
                    db.StreamLastOnlines.RemoveRange(lastOnlines);

                    db.SaveChanges();
                }

                if (streamToCheck.EmbedColor != 0)
                {
                    Console.WriteLine("add");

                    JobManager.AddJob(new TwitchUpdateJob(_client, _twitchService, streamToCheck),
                        s => s.WithName(stream.Id.ToString()).ToRunEvery(1).Minutes());
                }

                return;
            }

            // not live and was previously live
            if (stream == null && existingRecord != null)
            {
                using (var context = new DggContext())
                {
                    var responses = context.StreamNullResponses;
                    StreamNullResponse response;
                    if (responses.Any(r => r.UserId == existingRecord.UserId))
                    {
                        response = responses.First(r => r.UserId == existingRecord.UserId);
                        Console.WriteLine($"{response.NullResponseDate} + {DateTimeOffset.UtcNow.AddMinutes(-2)}");
                        if (response.NullResponseDate > DateTimeOffset.UtcNow.AddMinutes(-2))
                            return;
                    }
                    else
                    {
                        context.StreamNullResponses.Add(new StreamNullResponse
                        {
                            UserId = existingRecord.UserId,
                            NullResponseDate = DateTimeOffset.Now
                        });
                        context.SaveChanges();
                        return;
                    }

                    context.StreamNullResponses.Remove(response);
                    context.SaveChanges();
                }


                var channel = _client.GetChannel((ulong) streamToCheck.DiscordChannelId) as SocketTextChannel;
                var msgId = existingRecord.DiscordMessageId;

                if (streamToCheck.DeleteDiscordMessage && existingRecord.DiscordMessageId != 0)
                {
                    Log.Debug("removing message");
                    var msg =
                        channel.GetMessageAsync((ulong) existingRecord.DiscordMessageId).GetAwaiter().GetResult() as
                            RestUserMessage;

                    msg.DeleteAsync().GetAwaiter().GetResult();
                }
                else if (!streamToCheck.DeleteDiscordMessage && streamToCheck.EmbedColor != 0 &&
                         existingRecord.DiscordMessageId != 0)
                {
                    FinalMessageUpdateAsync(streamToCheck, existingRecord).GetAwaiter().GetResult();
                }

                if (streamToCheck.PinMessage && existingRecord.DiscordMessageId != 0)
                {
                    Log.Debug("removing Pin");
                    var msg =
                        channel.GetMessageAsync((ulong) existingRecord.DiscordMessageId).GetAwaiter().GetResult() as
                            RestUserMessage;
                    if (!(msg is null))
                        msg.UnpinAsync().GetAwaiter().GetResult();
                }

                using (var db = new DggContext())
                {
                    db.StreamLastOnlines.Add(new StreamLastOnline
                    {
                        UserId = streamToCheck.UserId,
                        FriendlyUsername = streamToCheck.FriendlyUsername,
                        LastOnlineAt = DateTime.UtcNow,
                        LastGame = existingRecord.CurrentGame
                    });

                    var games = db.StreamGames.Where(g => g.StreamId == existingRecord.StreamId);
                    db.StreamGames.RemoveRange(games);

                    Console.WriteLine($"Games: {db.SaveChanges()}");

                    var updateJob =
                        JobManager.AllSchedules.FirstOrDefault(u => u.Name == existingRecord.StreamId.ToString());
                    if (updateJob == null)
                        return;
                    Console.WriteLine("remove jhob");
                    JobManager.RemoveJob(updateJob.Name);

                    db.StreamRecords.Remove(existingRecord);
                    Console.WriteLine($"Records: {db.SaveChanges()}");
                }
            }
        }

        private async Task<long> SendMessageAsync(StreamToCheck streamToCheck, TwitchStream stream)
        {
            Embed embed = null;

            if (streamToCheck.EmbedColor != 0)
                embed = CreateEmbed(stream, (uint) streamToCheck.EmbedColor);

            var channel = _client.GetChannel((ulong) streamToCheck.DiscordChannelId) as SocketTextChannel;
            var resp = await channel.SendMessageAsync(streamToCheck.DiscordMessage, embed: embed);

            if (streamToCheck.PinMessage)
                await resp.PinAsync();

            return (long) resp.Id;
        }

        private Embed CreateEmbed(TwitchStream stream, uint inColor)
        {
            var now = DateTime.UtcNow;
            var cacheBuster =
                now.Year +
                now.Month.ToString() +
                now.Day +
                now.Hour +
                now.Minute / 10 % 10;

            var embed = new EmbedBuilder();
            var color = new Color(inColor);
            var author = new EmbedAuthorBuilder();
            var imgUrl = stream.Preview.Template.Replace("{width}", "640").Replace("{height}", "360") + "?" +
                         cacheBuster;

            author.Name = stream.Channel.DisplayName ?? stream.Channel.Name;
            author.Url = stream.Channel.Url;
            author.IconUrl = stream.Channel.Logo;

            var streamPlayingField = new EmbedFieldBuilder
            {
                Name = "Playing",
                Value = !string.IsNullOrWhiteSpace(stream.Game) ? stream.Game : "(no game)",
                IsInline = true
            };

            var streamViewersField = new EmbedFieldBuilder
            {
                Name = "Viewers",
                Value = stream.Viewers.ToString(),
                IsInline = true
            };

            embed.Color = color;
            embed.ImageUrl = imgUrl;
            embed.Title = !string.IsNullOrWhiteSpace(stream.Channel.Status) ? stream.Channel.Status : "(no title)";
            embed.Url = stream.Channel.Url;
            embed.Author = author;

            embed.AddField(streamPlayingField);
            embed.AddField(streamViewersField);

            return embed.Build();
        }

        private async Task FinalMessageUpdateAsync(StreamToCheck streamToCheck, StreamRecord record)
        {
            List<StreamGame> games;

            using (var db = new DggContext())
            {
                var latest = db.StreamGames
                    .FirstOrDefault(g => g.StreamId == record.StreamId && g.StopTime == null);

                if (latest != null)
                {
                    latest.StopTime = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }

                games = await db.StreamGames.Where(g => g.StreamId == record.StreamId).ToListAsync();
            }

            record.StartTime = DateTime.SpecifyKind(record.StartTime, DateTimeKind.Utc);

            var streamDuration = DateTime.UtcNow - record.StartTime;
            var startTime = TimeZoneInfo.ConvertTime(record.StartTime, Helpers.CentralTimeZone());
            var stopTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, Helpers.CentralTimeZone());

            var msg = new StringBuilder(streamToCheck.FriendlyUsername + " was live.\n\n");
            msg.Append("**Started at:** ");
            msg.Append(startTime.ToString("HH:mm"));
            msg.Append(" Central\n");
            msg.Append("**Ended at:** ");
            msg.Append(stopTime.ToString("HH:mm"));
            msg.Append(" Central\n");
            msg.Append("_(total of ");
            msg.Append(streamDuration.ToFriendlyString());
            msg.Append(")_\n\n");

            msg.Append("__Games Played__");

            foreach (var g in games)
            {
                // i dunno why it's putting an empty game for 0 minutes first each time,
                // but here's a quick fix lel
                if (string.IsNullOrEmpty(g.Game))
                    continue;

                g.StopTime = DateTime.SpecifyKind(g.StopTime.Value, DateTimeKind.Utc);
                g.StartTime = DateTime.SpecifyKind(g.StartTime, DateTimeKind.Utc);

                var duration = g.StopTime.Value - g.StartTime;
                msg.Append("\n**");
                msg.Append(g.Game);
                msg.Append(":** ");
                msg.Append(duration.ToFriendlyString());
            }

            try
            {
                var channel = _client.GetChannel((ulong) streamToCheck.DiscordChannelId) as SocketTextChannel;
                var existingMsg = await channel.GetMessageAsync((ulong) record.DiscordMessageId) as RestUserMessage;

                await existingMsg.ModifyAsync(m =>
                {
                    m.Content = msg.ToString();
                    m.Embed = null;
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[" + DateTime.UtcNow + "] TWITCH ERROR, FinalMessageUpdateAsync");
                Console.Error.WriteLine(ex.ToString());
                Console.Error.WriteLine(ex.InnerException?.ToString());
                Console.Error.WriteLine("------------");
                Console.Error.WriteLine();
            }
        }
    }
}