using System;
using System.Linq;
using DGGBot.Data;
using DGGBot.Data.Enitities.Twitch;
using DGGBot.Models;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using FluentScheduler;

namespace DGGBot.Services.Twitch
{
    public class TwitchUpdateJob : IJob
    {
        private readonly DiscordSocketClient _client;
        private readonly StreamToCheck _stream;
        private readonly TwitchService _twitchService;

        public TwitchUpdateJob(DiscordSocketClient client, TwitchService twitchService, StreamToCheck stream)
        {
            _client = client;
            _twitchService = twitchService;
            _stream = stream;
        }

        public void Execute()
        {
            
            StreamRecord record;
            using (var db = new DggContext())
            {
                record = db.StreamRecords.FirstOrDefault(sr => sr.UserId == _stream.UserId);
                if (record == null)
                    return;
            }

            var stream = _twitchService.GetTwitchStreamAsync(_stream.UserId).GetAwaiter().GetResult();
            if (stream == null)
                return;

            if (string.IsNullOrEmpty(stream.Game))
                stream.Game = "(no game)";

            if (record.CurrentGame != stream.Game)
                using (var db = new DggContext())
                {
                    var streamGame = db.StreamGames
                        .FirstOrDefault(g => g.StreamId == stream.Id && g.StopTime == null);

                    streamGame.StopTime = DateTime.UtcNow;
                    record.CurrentGame = stream.Game;

                    db.StreamGames.Add(new StreamGame
                    {
                        StreamId = stream.Id,
                        Game = stream.Game,
                        StartTime = DateTime.UtcNow
                    });

                    db.StreamRecords.Update(record);

                    db.SaveChanges();
                }

            var embed = CreateEmbed(stream, (uint) _stream.EmbedColor);

            try
            {
                var channel = _client.GetChannel((ulong) _stream.DiscordChannelId) as SocketTextChannel;
                var msg =
                    channel.GetMessageAsync((ulong) record.DiscordMessageId).GetAwaiter()
                        .GetResult() as RestUserMessage;

                msg.ModifyAsync(f => f.Embed = embed).GetAwaiter().GetResult();
                using (var context = new DggContext())
                {
                    var responses = context.StreamNullResponses;
                    if (responses.Any(f => f.UserId == _stream.UserId))
                        responses.Remove(responses.First(f => f.UserId == _stream.UserId));
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[" + DateTime.UtcNow + "] TWITCH ERROR, UpdateEmbedAsync");
                Console.Error.WriteLine(ex.ToString());
                Console.Error.WriteLine(ex.InnerException?.ToString());
                Console.Error.WriteLine("------------");
                Console.Error.WriteLine();
            }
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
    }
}