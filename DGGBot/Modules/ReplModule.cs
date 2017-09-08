using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DGGBot.Services.Eval;
using DGGBot.Services.Eval.ResultModels;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DGGBot.Modules
{
    public class Result
    {
        public object ReturnValue { get; set; }
        public string Exception { get; set; }
        public string Code { get; set; }
        public string ExceptionType { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public TimeSpan CompileTime { get; set; }
        public string ConsoleOut { get; set; }
        public string ReturnTypeName { get; set; }
    }

    [RequireOwner]
    public class ReplModule : ModuleBase
    {
        [Command("exec", RunMode = RunMode.Async)]
        [Alias("eval")]
        [Summary("Executes code!")]
        public async Task ReplInvoke([Remainder] string code)
        {
            if (code.Length > 1024)
            {
                await ReplyAsync("Eval failed: Code is greater than 1024 characters in length");
                return;
            }
            EvalResult result;
            var guildUser = Context.User as SocketGuildUser;
            var message = await Context.Channel.SendMessageAsync("Working...");

            var content = BuildContent(code);


            try
            {
                var eval = new CSharpEval();
                result = await eval.RunEvalAsync(content);
            }
            catch (TaskCanceledException)
            {
                await message.ModifyAsync(
                    a => { a.Content = $"Gave up waiting for a response from the REPL service."; });
                return;
            }
            catch (Exception ex)
            {
                await message.ModifyAsync(a => { a.Content = $"Exec failed: {ex.Message}"; });
                return;
            }


            var embed = BuildEmbed(guildUser, result);

            await message.ModifyAsync(a =>
            {
                a.Content = string.Empty;
                a.Embed = embed.Build();
            });

            await Context.Message.DeleteAsync();
        }

        private string BuildContent(string code)
        {
            var cleanCode = code.Replace("```csharp", string.Empty).Replace("```cs", string.Empty)
                .Replace("```", string.Empty);
            return
                Regex.Replace(cleanCode.Trim(), "^`|`$",
                    string.Empty); //strip out the ` characters from the beginning and end of the string
        }

        private EmbedBuilder BuildEmbed(SocketGuildUser guildUser, EvalResult parsedResult)
        {
            var returnValue = TrimIfNeeded(parsedResult.ReturnValue?.ToString() ?? " ", 1000);
            var consoleOut = TrimIfNeeded(parsedResult.ConsoleOut, 1000);
            var exception = TrimIfNeeded(parsedResult.Exception ?? string.Empty, 1000);

            var embed = new EmbedBuilder()
                .WithTitle("Eval Result")
                .WithDescription(string.IsNullOrEmpty(parsedResult.Exception) ? "Successful" : "Failed")
                .WithColor(string.IsNullOrEmpty(parsedResult.Exception) ? new Color(0, 255, 0) : new Color(255, 0, 0))
                .WithAuthor(a =>
                    a.WithIconUrl(Context.User.GetAvatarUrl()).WithName(guildUser?.Nickname ?? Context.User.Username))
                .WithFooter(a =>
                    a.WithText(
                        $"Compile: {parsedResult.CompileTime.TotalMilliseconds:F}ms | Execution: {parsedResult.ExecutionTime.TotalMilliseconds:F}ms"));

            embed.AddField(a => a.WithName("Code").WithValue(Format.Code(parsedResult.Code, "cs")));

            if (parsedResult.ReturnValue != null)
                embed.AddField(a => a.WithName($"Result: {parsedResult.ReturnTypeName ?? "null"}")
                    .WithValue(Format.Code($"{returnValue}", "txt")));

            if (!string.IsNullOrWhiteSpace(consoleOut))
                embed.AddField(a => a.WithName("Console Output")
                    .WithValue(Format.Code(consoleOut, "txt")));

            if (!string.IsNullOrWhiteSpace(parsedResult.Exception))
            {
                var diffFormatted = Regex.Replace(parsedResult.Exception, "^", "- ", RegexOptions.Multiline);
                embed.AddField(a => a.WithName($"Exception: {parsedResult.ExceptionType}")
                    .WithValue(Format.Code(diffFormatted, "diff")));
            }

            return embed;
        }

        private static string TrimIfNeeded(string value, int len)
        {
            if (value.Length > len)
                return value.Substring(0, len);

            return value;
        }
    }
}