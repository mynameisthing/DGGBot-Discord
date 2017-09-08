using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace DGGBot.Data.Migrations
{
    public partial class throttle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stream_game",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    game = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    start_time = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValue: new DateTime(2017, 9, 7, 23, 5, 6, 462, DateTimeKind.Utc)),
                    stop_time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    stream_id = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stream_game", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "stream_last_online",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    friendly_username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    last_game = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    last_online_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stream_last_online", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "stream_null_response",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    null_response_date = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stream_null_response", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "stream_record",
                columns: table => new
                {
                    stream_id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    current_game = table.Column<string>(type: "TEXT", nullable: false),
                    discord_message_id = table.Column<long>(type: "INTEGER", nullable: false),
                    start_time = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValue: new DateTime(2017, 9, 7, 23, 5, 6, 460, DateTimeKind.Utc)),
                    user_id = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stream_record", x => x.stream_id);
                });

            migrationBuilder.CreateTable(
                name: "stream_to_check",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    delete_discord_message = table.Column<bool>(type: "INTEGER", nullable: false),
                    discord_channel_id = table.Column<long>(type: "INTEGER", nullable: false),
                    discord_message = table.Column<string>(type: "TEXT", nullable: false),
                    discord_server_id = table.Column<long>(type: "INTEGER", nullable: false),
                    embed_color = table.Column<int>(type: "INTEGER", nullable: false),
                    frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    friendly_username = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    pin_message = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stream_to_check", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "throttle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    siscord_channel_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    module_name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_throttle", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tweet_record",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    author_name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    author_username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    profile_image_url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    text = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    tweet_id = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tweet_record", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "twitter_to_check",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    discord_channel_id = table.Column<long>(type: "INTEGER", nullable: false),
                    discord_server_id = table.Column<long>(type: "INTEGER", nullable: false),
                    frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    friendly_username = table.Column<string>(type: "TEXT", maxLength: 45, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_twitter_to_check", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "youtube_record",
                columns: table => new
                {
                    channel_id = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    author_icon_url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    author_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    author_url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    image_url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    published_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    video_description = table.Column<string>(type: "TEXT", maxLength: 550, nullable: false),
                    video_id = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    video_title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_youtube_record", x => x.channel_id);
                });

            migrationBuilder.CreateTable(
                name: "youtube_to_check",
                columns: table => new
                {
                    channel_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    discord_channel_id = table.Column<long>(type: "INTEGER", nullable: false),
                    discord_server_id = table.Column<long>(type: "INTEGER", nullable: false),
                    frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    friendly_username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_youtube_to_check", x => x.channel_id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stream_game");

            migrationBuilder.DropTable(
                name: "stream_last_online");

            migrationBuilder.DropTable(
                name: "stream_null_response");

            migrationBuilder.DropTable(
                name: "stream_record");

            migrationBuilder.DropTable(
                name: "stream_to_check");

            migrationBuilder.DropTable(
                name: "throttle");

            migrationBuilder.DropTable(
                name: "tweet_record");

            migrationBuilder.DropTable(
                name: "twitter_to_check");

            migrationBuilder.DropTable(
                name: "youtube_record");

            migrationBuilder.DropTable(
                name: "youtube_to_check");
        }
    }
}
