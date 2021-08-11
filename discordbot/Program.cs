using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace mafiabot
{
    class Program
    {
        public static void Main()
        => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandService _commands;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            CommandHandler handler = new CommandHandler(_client, _commands);

            await handler.InstallCommandsAsync();

            _client.Log += Log;

            // TEMPORARY SOLUTION
            string token = "NzE1NTk0MTIxMTg3ODE5NjEw.Xs_e9Q.41eA_Ptt4X5Vqy8ucKcB6Dun72E";

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            
            // Ensures this task will never complete
            await Task.Delay(-1);
        }

        static public Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }

    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        // Retrieve client and CommandService via constructor
        public CommandHandler(DiscordSocketClient client, CommandService commands)
        {
            _commands = commands;
            _client = client;
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into the command handler
            _client.MessageReceived += HandleCommandAsync;

            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: null);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            if (!(messageParam is SocketUserMessage message)) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            SocketCommandContext context = new SocketCommandContext(_client, message);

            // Execute the command with the command context, along with the service provider for precondition checks
            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: null);
        }
    }

    public class Bouncer
    {
        private readonly DiscordSocketClient _client;

        // Constructor
        public Bouncer(DiscordSocketClient client)
        {
            _client = client;
        }

        public void InstallBouncer()
        {
            _client.MessageReceived += CheckMessageAsync;
        }

        private Task CheckMessageAsync(SocketMessage messageParam)
        {
            throw new NotImplementedException();
        }
    }
}
