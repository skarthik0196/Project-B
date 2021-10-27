using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using ProjectB.Services;
using JikanDotNet;

namespace ProjectB
{
    class Bot
    {
        // The interface to the discord client
        public DiscordSocketClient m_discordClient { get; private set; }


        // Bot token
        // TODO: Get this value from an environment variable
        private string m_botToken;
        public async Task RunAsync()
        {
            IServiceProvider services = ConfigureServices();

            m_discordClient = services.GetRequiredService<DiscordSocketClient>();

            m_discordClient.Log += LogAsync;
            services.GetRequiredService<CommandService>().Log += LogAsync;

            m_discordClient.Ready += ReadyAsync;

            m_botToken = Environment.GetEnvironmentVariable("ProjectBToken");

            await m_discordClient.LoginAsync(TokenType.Bot, m_botToken);
            await m_discordClient.StartAsync();

            // Initialize the command handler
            await services.GetRequiredService<CommandHandler>().InitializeAsync();

            // Initialize the event handler
            await services.GetRequiredService<EmbedHandler>().InitializeAsync();

            // Block until program is closed.
            await Task.Delay(Timeout.Infinite);
        }

        private Task LogAsync(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            Console.WriteLine($"{m_discordClient.CurrentUser} has connected!");

            return Task.CompletedTask;
        }

        // Add the required services for the bot to function
        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<EmbedHandler>()
                .AddSingleton<Jikan>()
                .AddSingleton<MyAnimeListService>()
                .BuildServiceProvider();
        }
    }
}
