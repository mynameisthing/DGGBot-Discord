using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DGGBot.Data;
using DGGBot.Data.Enitities.Twitch;
using DGGBot.Extensions;
using DGGBot.Utilities.Attributes;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;

namespace DGGBot.Modules
{
    [Group("live")]
    [Alias("twitch")]
    [ChannelThrottle]
    public class TwitchModule : ModuleBase
    {
        [Command]
        public async Task GetLive([Remainder]string unused = null)
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
                msg = $"Destiny is offline. " +
                    "I'll have more info in the future, after the next time the stream goes live. " +
                    "Sorry!";
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

    }
}
