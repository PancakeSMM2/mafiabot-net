using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using Mafiabot.Jobs;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using static Mafiabot.Functions;
using static Mafiabot.Program;

namespace Mafiabot
{
    class Program
    {
        // Start MainAsync() on startup
        public static void Main()
        => MainAsync().GetAwaiter().GetResult();

        public static DiscordSocketClient _client; // The client
        public static CommandService _commands; // The command service

        public static async Task MainAsync()
        {
            // Make sure that required files exist
            string configPath = Environment.GetEnvironmentVariable("mafiabot-configPath") ?? "config.json"; // If config path has not been set via the mafiabot-configPath environment variable, default to "config.json"
            if (!File.Exists(configPath)) // If the config file does not exist
            {
                // Create a default config
                Dictionary<string, dynamic> defaultConfig = new();
                defaultConfig.Add("DefaultAvatarPath", "Avatar.png"); // Default file path
                defaultConfig.Add("ArchivalChannelsPath", "archivalChannels.json"); // Default file path
                defaultConfig.Add("ImagesOnlyPath", "imagesOnly.json"); // Default file path
                defaultConfig.Add("LogChannelsPath", "logChannels.json"); // Default file path
                defaultConfig.Add("PurgeChannelsPath", "purgeChannels.json"); // Default file path
                defaultConfig.Add("PrideFlagsPath", "prideFlags.json"); // Default file path

                defaultConfig.Add("Token", "YOUR-TOKEN-HERE"); // Field to input the twitch bot token
                defaultConfig.Add("GoogleProjectId", "YOUR-PROJECT-ID-HERE"); // Field to input the google project ID
                defaultConfig.Add("GoogleKey", "YOUR-GOOGLE-KEY-FILE-PATH-HERE"); // Field to input the google key's file path

                // Create and write text to the file
                File.WriteAllText(configPath, JsonConvert.SerializeObject(defaultConfig));

                // Send a line to the console
                Console.WriteLine($"No config file was found. A default one has been created at {configPath}.\nYou are required to input a few fields, such as your discord bot token and google key file path.\nThis program will automatically close in 60 seconds.");
                await Task.Delay(60000); // Delay 60 seconds
                // Stop the program
                return;
            }

            // If there is no archival channels file
            if (!File.Exists(Config.ArchivalChannelsPath))
            {
                // Create and write to the file
                File.WriteAllText(Config.ArchivalChannelsPath, JsonConvert.SerializeObject(new Dictionary<ulong, ulong>()));
            }

            // If there is no images only file
            if (!File.Exists(Config.ImagesOnlyPath))
            {
                // Create and write to the file
                File.WriteAllText(Config.ImagesOnlyPath, JsonConvert.SerializeObject(Array.Empty<ulong>()));
            }

            // If there is no log channels file
            if (!File.Exists(Config.LogChannelsPath))
            {
                // Create and write to the file
                File.WriteAllText(Config.LogChannelsPath, JsonConvert.SerializeObject(Array.Empty<ulong>()));
            }

            // If there is no pride flags file
            if (!File.Exists(Config.PrideFlagsPath))
            {
                // Create and write to the file
                File.WriteAllText(Config.PrideFlagsPath, JsonConvert.SerializeObject(Array.Empty<PrideFlag>()));
            }

            // If there is no purge channels file
            if (!File.Exists(Config.PurgeChannelsPath))
            {
                // Create and write to the file
                File.WriteAllText(Config.PurgeChannelsPath, JsonConvert.SerializeObject(Array.Empty<ulong>()));
            }

            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                ExclusiveBulkDelete = false // Determines whether bulk deletes will be excluded from the MessageDeleted event
            }); // Create the client
            _commands = new CommandService(new CommandServiceConfig()
            {
                IgnoreExtraArgs = true, // Whether to ignore extra provided parameters for commands
                CaseSensitiveCommands = false // Whether commands should be case-sensitive
            }); // Create the command service

            CommandHandler handler = new(_client, _commands); // Create the command handler
            Bouncer bouncer = new(_client); // Create the bouncer
            Archiver archiver = new(_client); // Create the archiver

            Environment.SetEnvironmentVariable("GOOGLE_CLOUD_PROJECT", Config.GoogleProjectId); // Set the "GOOGLE_CLOUD_PROJECT" environment variable
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Config.GoogleKey); // Set the "GOOGLE_APPLICATION_CREDENTIALS" environment variable

            // Install the handler, bouncer, and archiver
            await handler.InstallCommandsAsync();
            bouncer.InstallBouncer();
            archiver.InstallArchiver();

            _client.Log += LogAsync; // Install the log function

            string token = Config.Token; // Get the bot's token, from the config file

            // Login and start the client
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Set the log provider for Quartz
            LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());

            // Create a scheduler factory
            StdSchedulerFactory factory = new();
            // Create a scheduler
            IScheduler scheduler = await factory.GetScheduler();

            // Start the scheduler
            await scheduler.Start();

            // Create the avatar reset job
            IJobDetail resetAvatar = JobBuilder.Create<AvatarResetJob>()
                .WithIdentity("resetAvatar", "mafiabot")
                .Build();

            // Create the activity reset job
            IJobDetail resetActivity = JobBuilder.Create<ActivityResetJob>()
                .WithIdentity("resetActivity", "mafiabot")
                .Build();

            // Create the channel purge job
            IJobDetail channelPurge = JobBuilder.Create<ChannelPurgeJob>()
                .WithIdentity("channelPurge", "mafiabot")
                .Build();

            // Create three triggers (one for each job) set to trigger once daily at midnight UTC
            ITrigger daily = TriggerBuilder.Create()
                .WithIdentity("daily", "mafiabot")
                .WithCronSchedule("0 0 0 ? * */1")
                .StartNow()
                .Build();
            ITrigger daily2 = TriggerBuilder.Create()
                .WithIdentity("daily2", "mafiabot")
                .WithCronSchedule("0 0 0 ? * */1")
                .StartNow()
                .Build();
            ITrigger daily3 = TriggerBuilder.Create()
                .WithIdentity("daily3", "mafiabot")
                .WithCronSchedule("0 0 0 ? * */1")
                .StartNow()
                .Build();

            // Schedule all three jobs
            await scheduler.ScheduleJob(resetAvatar, daily);
            await scheduler.ScheduleJob(resetActivity, daily2);
            await scheduler.ScheduleJob(channelPurge, daily3);

            // Ensure this task will never complete
            await Task.Delay(-1);
        }

        // Creates a function to provide to the discord client and to Quartz, logging their messages both to the console and to the log channels
        static public async Task LogAsync(LogMessage msg)
        {
            // Execute asynchronously
            await Task.Run(async () =>
            {
                // Write the message to the console
                Console.WriteLine(msg.ToString());

                // Try-catch block
                try
                {
                    // Get all of the log channel IDs
                    ulong[] channels = await GetUlongsFromJSONAsync(Config.LogChannelsPath);

                    // Create an embed
                    EmbedBuilder embed = new EmbedBuilder()
                        .WithColor(GetRainbowColor((int)msg.Severity)) // Sets the color based on the severity of the message
                        .WithCurrentTimestamp() // Sets the timestamp
                        .WithDescription($"`{msg.ToString()}`"); // Set the description

                    // If the message has an internal exception
                    if (msg.Exception is not null)
                    {
                        // Set the embed's footer to the exception (to string)
                        embed.WithFooter(msg.Exception.ToString());
                    }

                    // For each of the log channels
                    foreach (ulong channelId in channels)
                    {
                        // Get the channel
                        SocketChannel channel = _client.GetChannel(channelId);
                        // If the channel isn't a guild text channel, skip this channel
                        if (channel is not SocketTextChannel textChannel) continue;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        // Send the embed
                        textChannel.SendMessageAsync(embed: embed.Build());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }
                }
                catch (Exception ex) // In case of an exception
                {
                    // Log the log failure to the console
                    Console.WriteLine(new LogMessage(LogSeverity.Warning, "Mafiabot", "Failed to log message to log channels", ex).ToString());
                }
            });
        }

        // Create a ConsoleLogProvider to give to Quartz
        private class ConsoleLogProvider : ILogProvider
        {
            // Returns a quartz logger
            public Logger GetLogger(string name)
            {
                // Return a lambda
                return (level, func, exception, parameters) =>
                {
                    // If the log message is info or more severe, and there is a message associated with it
                    if (level >= LogLevel.Info && func != null)
                    {
                        // Define a variable to store the log severity (as needed for Discord.NET's LogMessage object that is used by log async)
                        LogSeverity severity = LogSeverity.Critical;
                        // Switch statement based on the Quartz log level
                        switch (level)
                        {
                            // Trace -> Verbose
                            case LogLevel.Trace:
                                severity = LogSeverity.Verbose;
                                break;
                            // Debug -> Debug
                            case LogLevel.Debug:
                                severity = LogSeverity.Debug;
                                break;
                            // Info -> Info
                            case LogLevel.Info:
                                severity = LogSeverity.Info;
                                break;
                            // Warn -> Warning
                            case LogLevel.Warn:
                                severity = LogSeverity.Warning;
                                break;
                            // Error -> Error
                            case LogLevel.Error:
                                severity = LogSeverity.Error;
                                break;
                            // Fatal -> Critical
                            case LogLevel.Fatal:
                                severity = LogSeverity.Critical;
                                break;
                        }

                        // Create a LogMessage and log it
                        LogAsync(new LogMessage(severity, "Quartz", func(), exception)).Wait();
                    }
                    // Return a success
                    return true;
                };
            }

            // "Implements" OpenNestedContext
            public IDisposable OpenNestedContext(string message)
            {
                throw new NotImplementedException();
            }

            // "Implements" OpenMappedContext
            public IDisposable OpenMappedContext(string key, object value, bool destructure = false)
            {
                throw new NotImplementedException();
            }
        }

        // Class to hold the configuration info
        public static class Config
        {
            // Constructor
            static Config()
            {
                // Set the configPath, either the environment variable mafiabot-configPath (if it is set) or default to "config.json"
                string configPath = Environment.GetEnvironmentVariable("mafiabot-configPath") ?? "config.json";
                // Read the config as a string key dynamic value Dictionary
                Dictionary<string, dynamic> loaded = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(File.ReadAllText(configPath));

                // Set all properties to the loaded config's properties
                DefaultAvatarPath = loaded["DefaultAvatarPath"];
                ArchivalChannelsPath = loaded["ArchivalChannelsPath"];
                ImagesOnlyPath = loaded["ImagesOnlyPath"];
                LogChannelsPath = loaded["LogChannelsPath"];
                PrideFlagsPath = loaded["PrideFlagsPath"];
                PurgeChannelsPath = loaded["PurgeChannelsPath"];

                Token = loaded["Token"];
                GoogleProjectId = loaded["GoogleProjectId"];
                GoogleKey = loaded["GoogleKey"];
            }

            public static string DefaultAvatarPath; // The file path to the default avatar
            public static string ArchivalChannelsPath; // The file path to the archival channels
            public static string ImagesOnlyPath; // The file path to the images only channels
            public static string LogChannelsPath; // The file path to the log channels
            public static string PrideFlagsPath; // The file path to the pride flags
            public static string PurgeChannelsPath; // The file path to the purge channels

            public static string Token; // The bot's token
            public static string GoogleProjectId; // The Google Cloud project ID
            public static string GoogleKey; // The key file path for interaction with Google Cloud Translate
        }
    }

    public class CommandHandler
    {
        // Variables to store the client and the CommandService
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

            // Load the command modules
            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: null);
        }

        public async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            if (messageParam is not SocketUserMessage message) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands, unless prefixed with the string "[NO_BOT_OVERRIDE]"
            if (!(message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos) ||
                message.HasStringPrefix("[NO_BOT_OVERRIDE]", ref argPos)) ||
                (message.Author.IsBot && !message.HasStringPrefix("[NO_BOT_OVERRIDE]", ref argPos)))
                return;

            // Create a WebSocket-based command context based on the message
            SocketCommandContext context = new(_client, message);

            // Execute the command with the command context, along with the service provider for precondition checks
            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: null);
        }
    }

    // The ImagesOnly bouncer class
    public class Bouncer
    {
        // A variable to store the client
        private readonly DiscordSocketClient _client;

        // Get the client via a constructor
        public Bouncer(DiscordSocketClient client)
        {
            _client = client;
        }

        // Called to install the bouncer
        public void InstallBouncer()
        {
            // Hook the MessageReceived event into the bouncer
            _client.MessageReceived += CheckMessageAsync;
        }

        // The function used to check each message and ensure it's permitted
        private async Task CheckMessageAsync(SocketMessage messageParam)
        {
            // Execute asynchronously
            await Task.Run(async () =>
            {
                if (messageParam is not SocketUserMessage message) return; // If the message is not a SocketUserMessage, return

                int argPos = 0; // Declares an int, to use to store the number of arguments. Unused, but mandatory

                // If the message starts with the character or mention prefix, or was sent by a bot, return.
                if (message.HasCharPrefix('!', ref argPos) ||
                    message.HasMentionPrefix(_client.CurrentUser, ref argPos) ||
                    message.Author.IsBot)
                    return;

                if (message.Channel is not SocketGuildChannel channel) return; // If the message wasn't sent in a guild channel, return.

                // Check whether the channel the message was sent in is marked images only
                bool inImagesOnlyChannel = await CheckUlongFromJSONAsync(channel.Id, Config.ImagesOnlyPath);
                // If it is in an ImagesOnly channel, wait 3 seconds (so that embeds have a chance to load)
                if (inImagesOnlyChannel)
                {
                    // Wait 3 seconds
                    await Task.Delay(3000);
                }

                // Get the message
                RestUserMessage updatedMessage = (RestUserMessage)await message.Channel.GetMessageAsync(message.Id);

                if (inImagesOnlyChannel && updatedMessage.Attachments.Count == 0 && updatedMessage.Embeds.Count == 0) // If it is, and it has no attachments, delete the message with an audit log reason.
                {
                    // Create a RequestOptions
                    RequestOptions options = new()
                    {
                        AuditLogReason = $"Non-image message of content {updatedMessage.Content} sent in image-only channel {channel.Name}." // Add an audit log reason
                    };

                    // Delete the message
                    await message.DeleteAsync(options);
                }
            });

        }
    }

    // The Archiver class
    public class Archiver
    {
        // Variable to store the client
        private readonly DiscordSocketClient _client;

        // Constructor to get the client
        public Archiver(DiscordSocketClient client)
        {
            _client = client;
        }

        // Called to install the archiver
        public void InstallArchiver()
        {
            // Hook the MessageReceived event into the archiver
            _client.MessageReceived += ArchiveMessageAsync;
        }

        // Called when a message is received by the client
        private async Task ArchiveMessageAsync(SocketMessage messageParam)
        {
            // Execute asynchronously
            await Task.Run(async () =>
            {
                // If the message was not sent by a user, return
                if (messageParam is not SocketUserMessage message) return;

                // Check if the message channel has a target archival channel
                ulong target = await GetChannelFromJSONDictionaryAsync(message.Channel.Id, "./archivalChannels.json");

                // If it does
                if (target != default)
                {
                    // Get the target channel (and return if the target channel is not a guild text channel
                    SocketChannel channel = _client.GetChannel(target);
                    if (channel is not SocketTextChannel targetChannel) return;

                    // Get the source channel (and return if it is not a guild text channel)
                    if (message.Channel is not SocketTextChannel sourceChannel) return;

                    SocketGuildUser author = sourceChannel.Guild.GetUser(message.Author.Id); // Get the author's GuildUser from the source guild
                    SocketRole highestRole = sourceChannel.Guild.EveryoneRole; // Create a variable to store the author's highest role, defaulting to the guild's @everyone role.

                    // For each role that the author has
                    foreach (SocketRole role in author.Roles)
                    {
                        // If the role's position is greater than or equal to than the currently stored highest role's position
                        if (role.Position >= highestRole.Position)
                        {
                            // Store the role as the highest role
                            highestRole = role;
                        }
                    }

                    // Get the author's avatar url
                    string avatar = author.GetAvatarUrl();
                    avatar ??= author.GetDefaultAvatarUrl(); // If the user has no avatar, get their default avatar

                    // Get the author's nickname, or if they have none, get their username
                    string nickname = author.Nickname != null ? $"{author.Nickname} ({author.Username})" : author.Username;

                    // Create an embed
                    EmbedBuilder embed = new EmbedBuilder()
                        .WithAuthor(nickname, avatar, message.GetJumpUrl()) // Set the author to be the message's author
                        .WithColor(highestRole.Color) // Set the color to be the highest role's color
                        .WithTimestamp(message.CreatedAt) // Set the timestamp to be when the message was sent
                        .WithDescription(message.Content); // Set the embed's description to be the message content

                    // If the message has an attachment
                    if (message.Attachments.Count != 0)
                    {
                        // For each of the attachments
                        foreach (Attachment attachment in message.Attachments)
                        {
                            // If the attachment is an image (only images have a width)
                            if (attachment.Width != 0) embed.WithImageUrl(attachment.Url); // Set it to be the embed's image
                        }
                    }

                    // Reply with the embed
                    await targetChannel.SendMessageAsync(embed: embed.Build());
                }
            });
        }
    }
}
