using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;

namespace PawSharp.DocBot
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var cfg = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var token = cfg["DOCBOT_TOKEN"] ?? Environment.GetEnvironmentVariable("DOCBOT_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("DOCBOT_TOKEN is not set. Please set environment variable or appsettings.json");
                return;
            }

            var referenceFolder = cfg["REFERENCE_FOLDER"] ?? Environment.GetEnvironmentVariable("REFERENCE_FOLDER") ?? "PawSharp-Reference";

            var discord = new DiscordClient(new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot
            });

            var slash = discord.UseSlashCommands();
            var docsService = new DocsService(referenceFolder);
            // register static service and command module type
            Commands.DocsModule.Docs = docsService;
            slash.RegisterCommands<Commands.DocsModule>();

            await discord.ConnectAsync();
            Console.WriteLine("DocBot connected. Press Ctrl+C to exit.");
            await Task.Delay(-1);
        }
    }
}
