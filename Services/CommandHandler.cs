using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace ProjectB.Services
{
    public class CommandHandler
    {
        private readonly CommandService      m_commandService;
        private readonly DiscordSocketClient m_discordClient;
        private readonly IServiceProvider    m_services;

        private static string m_commandPrefix = "X.";

        // Constrcutor that requires a configured service provider to extract other dependencies for internal use.
        public CommandHandler(IServiceProvider services)
        {
            m_commandService = services.GetRequiredService<CommandService>();
            m_discordClient  = services.GetRequiredService<DiscordSocketClient>();
            m_services       = services;

            // Hook the required functions to the discord client
            m_discordClient.MessageReceived  += HandleCommand;
            m_commandService.CommandExecuted += CommandExecutedAsync;
        }

        public async Task InitializeAsync()
        {
            // Register modules that are public and inherit ModuleBase<T>
            await m_commandService.AddModulesAsync(Assembly.GetEntryAssembly(), m_services);
        }

        public async Task HandleCommand(SocketMessage socketMessage)
        {
            // Only respond to users, Ignore Bots and other system messages
            if ((socketMessage is SocketUserMessage message) &&
               (message.Source == MessageSource.User))
            {
                // Holds the offset where the prefix ends
                int argPos = 0;

                // ProjectB responds to the command "NJ." (Not case sensitive) and direct mentions
                bool isCommandMessage = message.HasStringPrefix(m_commandPrefix, ref argPos, StringComparison.OrdinalIgnoreCase) ||
                                        message.HasMentionPrefix(m_discordClient.CurrentUser, ref argPos);

                if (isCommandMessage)
                {
                    ICommandContext context = new SocketCommandContext(m_discordClient, message);

                    // This will normally return a result but we will handle it in CommandExecutedAsync instead
                    await m_commandService.ExecuteAsync(context, argPos, m_services);
                }
                                        
            }
        }


        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // We ignore errors because of incorrectly specified commands (command.IsSpecified == false)
            bool skipError = (command.IsSpecified == false) || result.IsSuccess;

            if (skipError == false)
            {
                await context.Channel.SendMessageAsync($"Encountered an error : {result}");
            }

        }
    }
}
