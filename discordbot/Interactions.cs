using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Google.Cloud.Translation.V2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Mafiabot.Functions;
using static Mafiabot.Program;

namespace Mafiabot
{
    public class Interactions : InteractionModuleBase<SocketInteractionContext>
    {
        /** 
         * The Ping command.
        */
        [SlashCommand("ping", "Replies with 🏓, and the delay")]
        public async Task PingAsync()
        {
            // Store when the command message was sent, in milliseconds
            long commandSentMs = Context.Interaction.CreatedAt.ToUnixTimeMilliseconds();
            // Calculate the delay between when the command was sent and now
            long delay = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - commandSentMs;

            // Respond to the message with the number of milliseconds of delay, and then a ping-pong paddle emoji
            await RespondAsync($"{delay} ms delay 🏓", ephemeral: true);
        }

        /** 
         * The ChangeStatus command.
         * To-do:
         * - Add a way to reset the bot's status
         */
        [MessageCommand("Change Status to Message")]
        public async Task ChangeStatusAsync(SocketUserMessage message) => await ChangeStatusAsync(message.Content); // Execute ChangeStatusAsync() with the message's content

        // Define an enum for all valid bot activity types
        public enum BotActivityType
        {
            // Bots can have any activity type other than CustomStatus or Streaming
            Playing = ActivityType.Playing, 
            Listening = ActivityType.Listening,
            Watching = ActivityType.Watching,
            Competing = ActivityType.Competing
        }

        [SlashCommand("changestatus", "Changes the bot's status.")]
        public async Task ChangeStatusAsync([Summary("status", "The status text to set.")] string newActivity, [Summary("type", "The status type to set.")] BotActivityType activityType = BotActivityType.Playing)
        {
            Console.WriteLine((ActivityType)activityType);
            // Set the activity (of given type) to the supplied string
            await Context.Client.SetActivityAsync(new Game(newActivity, (ActivityType)activityType));
            // Create a string to store the response emote in (defaulting to 🎮, for Playing)
            string responseEmote = "🎮";
            // Switch statement, holding each of the different activity types
            switch (activityType)
            {
                case BotActivityType.Playing: // ActivityType.Playing and 🎮
                    // Set the emoji to be 🎮, indicating the activity type
                    responseEmote = "🎮";
                    break;
                case BotActivityType.Listening: // ActivityType.Listening and 🎧
                    // Set the emoji to be 🎧, indicating the activity type
                    responseEmote = "🎧";
                    break;
                case BotActivityType.Watching: // ActivityType.Watching and 📺
                    // Set the emoji to be 📺, indicating the activity type
                    responseEmote = "📺";
                    break;
                case BotActivityType.Competing: // ActivityType.Competing and 🥊
                    // Set the emoji to be 🥊, indicating the activity type
                    responseEmote = "🥊";
                    break;
            }

            // Respond with the emotes
            await RespondAsync("✅" + responseEmote, ephemeral: true);
        }

        /** 
         * The ImagesOnly command.
         */
        [SlashCommand("imagesonly", "Sets a channel to be images only.")]
        [RequireContext(ContextType.Guild)]
        public async Task ImagesOnlyAsync([Summary("channel", "The channel to set to be image-only; this channel, if not provided.")] ITextChannel channel = null)
        {
            // Execute asynchronously
            await Task.Run(async () =>
            {
                // If a channel is not provided, default to the active channel
                ulong imagesOnlyId = channel == null ? Context.Channel.Id : channel.Id;
                // Toggle the channel the command was sent from in imagesOnly.json, and store whether it was added or removed
                bool added = await ToggleUlongFromJSONAsync(imagesOnlyId, Config.ImagesOnlyPath);

                // Respond with the emotes, ✅ if the channel is now images only, 🚫 if the channel is no longer images only, followed by 🖼
                await RespondAsync((added ? "✅" : "🚫") + "🖼", ephemeral: true);
            });
        }

        /** 
         * The ChangePfp command.
         */
        [MessageCommand("Change PFP")]
        public async Task ChangePfpAsync(SocketUserMessage message)
        {
            // Execute asynchronously
            await Task.Run(async () =>
            {
                string url; // Declare an empty variable to hold the attachment's URL
                // Try-catch block to try and get the URL
                try
                {
                    // Set the URL to be the URL of the first attachment that has a designated width (which only images have)
                    url = message.Attachments.First((Attachment attached) => attached.Width != null).Url;
                }
                catch
                {
                    // If that fails (due to not finding a valid attachment), respond with 🚫📸 to indicate that it did not find an image
                    await RespondAsync("🚫📸", ephemeral: true);
                    // Then return
                    return;
                }

                // Attempt to change the bot's avatar, and store whether it was successful
                bool successful = await ChangeAvatarAsync(url, Context.Client.CurrentUser);

                if (successful) await RespondAsync("✅", ephemeral: true); // If successful, respond with ✅ (indicating as such)
                else await RespondAsync("🚫⏲️", ephemeral: true); // If unsuccessful, respond with 🚫⏲️ (indicating that avatar changes are currently on cooldown)
            });
        }

        /** 
         * The ResetPfp command.
         */
        [SlashCommand("resetpfp", "Resets the bot's profile picture to the default.")]
        public async Task ResetPfpAsync()
        {
            // Reset the avatar, and store whether it was successful
            bool successful = await ResetAvatarAsync(Context.Client.CurrentUser);

            if (successful) await RespondAsync("✅", ephemeral: true); // If successful, respond with ✅ (indicating as such)
            else await RespondAsync("🚫⏲️", ephemeral: true); // If unsuccessful, respond with 🚫⏲️ (indicating that avatar changes are currently on cooldown)
        }

        /**
         * The TimeDisplay command.
         */
        [MessageCommand("Time Display")]
        public async Task TimeConvertAsync(SocketUserMessage message) => await TimeConvertAsync(message.Content); // Run TimeConvertAsync with the message's content

        [SlashCommand("timedisplay", "Displays the given time with Discord's timestamp formatting")]
        public async Task TimeConvertAsync([Summary("time", "The time to display, such as\"8:00 AM\" or \"May 8, 1988 5:49 PM\", in UTC")] string time)
        {
            // Parse the provided time into a DateTime object
            DateTimeOffset parsed;
            try
            {
                // Try to parse the provided string into a DateTimeOffset
                parsed = DateTimeOffset.Parse(time, null, System.Globalization.DateTimeStyles.AllowWhiteSpaces);
            }
            catch (Exception)
            {
                // If the parse fails, respond
                await RespondAsync("📆❓", ephemeral: true);
                // Return
                return;
            }

            // Respond with both timestamps
            await RespondAsync(embed: new EmbedBuilder()
                .WithColor(GetRainbowColor()) // Set the color
                .AddField("Exact Time", $"<t:{parsed.ToUnixTimeSeconds()}:f>") // Add a field with the exact timestamp
                .AddField("Relative Time", $"<t:{parsed.ToUnixTimeSeconds()}:R>") // Add a field with the relative timestamp
                .WithTimestamp(parsed) // Set the timestamp
                .Build());
        }

        /**
         * The Pride command.
         */

        // Define an enum containing every pride flag, for autocompletion
        public enum PrideFlags
        {
            Lesbian = 0,
            Gay = 1,
            Bisexual = 2,
            Pansexual = 3,
            Asexual = 4,
            Demisexual = 5,
            Demilesbian = 6,
            Omnisexual = 7,
            Abrosexual = 8,
            Gay_Biromantic = 9,
            Aromantic = 10,
            AroAce = 11,
            Transgender = 12,
            Nonbinary = 13,
            Genderqueer = 14,
            Genderfluid = 15,
            Agender = 16,
            Aspergers = 17,
            Therian = 18
        }

        [SlashCommand("pride", "Sends a specified pride flag")]
        public async Task PrideFlagAsync([Summary("flag", "The flag to display")] PrideFlags flagName)
        {
            // Get the pride flag
            PrideFlag flag = await GetPrideFlagAsync(flagName.ToString().Replace('_', '-'));

            // Construct a reply embed
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(GetRainbowColor()) // Random rainbow color
                .WithImageUrl(flag.Url) // Display the flag
                .WithAuthor("Pride", "https://media.discordapp.net/attachments/716108846816297040/823648970215522304/image.png"); // Set the author

            // Reply with the embed
            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("pride-list", "Lists all supported pride flags and their aliases")]
        public async Task PrideFlagAsync()
        {
            // Get the pride flag names
            string names = await GetPrideFlagNamesAsync();

            // Respond with a reply embed
            await RespondAsync(embed: new EmbedBuilder()
                .WithColor(GetRainbowColor()) // Random rainbow color
                .WithAuthor("Pride", "https://media.discordapp.net/attachments/716108846816297040/823648970215522304/image.png") // Set the author
                .AddField("!pride <Flag>", "\u200b") // First field, with the command syntax
                .AddField("Flag", "One of the supported pride flags, as listed below. If you request a flag that isn't supported, you'll just get the progressive flag. If you want a flag added to this, lemme know!") // Second field, describing the Flag argument
                .AddField("SupportedFlags", names) // Third field, with the pride flag names
                .WithImageUrl("https://cdn.discordapp.com/attachments/716108846816297040/823668797248372766/Z.png") // Send the progressive flag
                .Build(), ephemeral: true);
        }

        /**
         * The Translate command.
         */
        // Create the translation client with default credentials
        private static readonly TranslationClient translate = TranslationClient.Create();

        [MessageCommand("Translate")]
        public async Task TranslateAsync(SocketUserMessage message) => await TranslateAsync(message.Content);

        [SlashCommand("translate", "Repeatedly translates a given string")]
        public async Task TranslateAsync([Summary("text", "The text to translate")] string text, [Summary("cycles", "The number of cycles to translate for, maximum of 50")][MinValue(1)][MaxValue(50)] int cycles = 5)
        {
            // Execute asynchronously
            await Task.Run(async () =>
            {
                // Sanity check for number of cycles
                if (cycles > 50)
                {
                    // Respond
                    await RespondAsync(embed: new EmbedBuilder()
                        .WithAuthor("Translate", "https://cdn.discordapp.com/attachments/716108846816297040/823295348474642492/1024px-Google_Translate_logo.png")
                        .WithColor(GetRainbowColor())
                        .WithDescription("Cycles are limited at a maximum of 50\nNote that increased cycles have diminishing returns")
                        .Build(), ephemeral: true);
                    // Don't execute the rest of the command
                    return;
                }


                // Get all languages (with names in English)
                IList<Language> langs = await translate.ListLanguagesAsync(LanguageCodes.English);
                // Copy the provided text variable to a new string called translate text
                string translateText = text;

                // Detect the language used in text
                Detection detection = await translate.DetectLanguageAsync(text);
                string detectedLangCode = detection.Language; // Store the detected language code

                // Create a variable to store the provided language, defaulting to English
                Language firstLanguage = new("English", "en");
                // For each language (as listed before)
                foreach (Language testLang in langs)
                {
                    // If this language has the same code as the detected language
                    if (testLang.Code == detectedLangCode)
                    {
                        // Set the first language to this language
                        firstLanguage = testLang;
                    }
                }

                // Construct an embed
                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(GetRainbowColor()) // Set the color
                    .WithAuthor("Translate", "https://cdn.discordapp.com/attachments/716108846816297040/823295348474642492/1024px-Google_Translate_logo.png") // Set the author to be "Translate"
                    .AddField("Text", text) // Add a field to show the current state of the text as it gets translated
                    .AddField("Progress", $"0 of {cycles}", true) // Add an inline field to show the number of cycles completed
                    .AddField("Translating to:", "\u200B", true) // Add an inline field to show the language currently being translated to
                    .AddField("Translating from:", firstLanguage.Name, true) // Add an inline field to show the language currently being translated from
                    .AddField("Translating sequence", firstLanguage.Name); // Add a field to show the sequence of translations

                // Send the embed
                await RespondAsync(embed: embed.Build());

                // Create a variable to store the language the text is currently in
                Language previousLanguage = firstLanguage;
                // For loop, repeats a number of times equal to cycles
                for (int i = 0; i < cycles; i++)
                {
                    Language target; // Create a variable to store the target language
                    // Do-while loop, repeats if the target's language code equals the previous language's code, to ensure that the text is never translated from its current language to its current language
                    do
                    {
                        // Randomly assigns target to one of the languages
                        target = langs[RandomInt(langs.Count)];
                    } while (target.Code == previousLanguage.Code);

                    // Translates the text (from the previous language to the target)
                    TranslationResult result = translate.TranslateText(translateText, target.Code, previousLanguage.Code);
                    translateText = result.TranslatedText; // Overwrite the previous value of translateText with the result

                    // Reconstruct the embed
                    embed = new EmbedBuilder()
                        .WithColor(GetRainbowColor()) // Set the color
                        .WithAuthor("Translate", "https://cdn.discordapp.com/attachments/716108846816297040/823295348474642492/1024px-Google_Translate_logo.png") // Set the author
                        .AddField("Text", translateText) // The new value of translateText
                        .AddField("Progress", $"{i + 1} of {cycles}", true) // The number of cycles that have been completed (+1 since this one has finished)
                        .AddField("Translating to:", target.Name, true) // Show the language that was translated to
                        .AddField("Translating from:", firstLanguage.Name, true) // Show the language that was translated from
                        .AddField("Translating sequence", $"{embed.Fields[4].Value} -> {target.Name}"); // Update the translate sequence, appending the most recent language to the previous value

                    // Edit the reply message to show the new embed, without waiting for it to finish editing (for performance)
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    Context.Interaction.ModifyOriginalResponseAsync((properties) =>
                    {
                        properties.Embed = embed.Build();
                    });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                    // Update the previous language
                    previousLanguage = target;

                    // Loop
                }

                // After translation is finished
                // If the text is currently not in its original language
                if (previousLanguage != firstLanguage)
                {
                    // Translate the text into the original language
                    TranslationResult result = translate.TranslateText(translateText, firstLanguage.Code, previousLanguage.Code);
                    translateText = result.TranslatedText; // Store the result
                }

                // Reply with a second embed
                await FollowupAsync(embed: new EmbedBuilder()
                    .WithAuthor("Translate", "https://cdn.discordapp.com/attachments/716108846816297040/823295348474642492/1024px-Google_Translate_logo.png") // Set the author
                    .WithColor(GetRainbowColor()) // Set the color
                    .AddField("Output", translateText) // Add a field with the final text
                    .Build());
            });
        }

        /**
         * The Archive command.
         */
        [SlashCommand("archive", "Sets one given channel to be archived in another given channel")]
        public async Task MarkArchiveAsync([Summary("target-channel", "The channel to archive to")] SocketTextChannel targetChannel, [Summary("source-channel", "The channel to archive from, defaulting to this channel")] SocketTextChannel sourceChannel = null)
        {
            // If there is no provided source channel, default to the contextual channel
            if (sourceChannel == null) sourceChannel = (SocketTextChannel)Context.Channel;

            // If both channels are referenced via a channel link, extract the id from it and execute as normal
            await MarkArchiveAsync(targetChannel.Id, sourceChannel.Id);
        }

        [SlashCommand("archive-id", "Sets one given channel to be archived in another given channel")]
        public async Task MarkArchiveAsync([Summary("target-channel-id", "The ID of the channel to archive to")] ulong targetChannelId, [Summary("source-channel-id", "The ID of the channel to archive from, defaulting to this channel")] ulong? sourceChannelId = null)
        {
            // If there is no provided source channel id, default to the contextual channel id
            if (sourceChannelId == null) sourceChannelId = Context.Channel.Id;
            // Convert the nullable souceChannelId to the non-nullable sourceId. Should never return, but just in case.
            if (sourceChannelId is not ulong sourceId) return;

            // Assign the target channel to the source channel in archivalChannels.json
            await AssignUlongToJSONDictionaryAsync(sourceId, targetChannelId, Config.ArchivalChannelsPath);

            // Convert the SocketChannel returned by GetChannel and convert it to a SocketTextChannel. If it isn't a guild text channel, return.
            if (Context.Client.GetChannel(sourceId) is not SocketTextChannel sourceChannel)
            {
                // Respond with an error message
                await RespondAsync("❌ Source channel must be a text channel.", ephemeral: true);
                return;
            }

            // Respond  
            await RespondAsync("✅", ephemeral: true);

            // Reply
            IUserMessage reply = await sourceChannel.SendMessageAsync(embed: new EmbedBuilder()
                .WithAuthor("Archival", "https://external-content.duckduckgo.com/iu/?u=https%3A%2F%2Fwww.emoji.co.uk%2Ffiles%2Ftwitter-emojis%2Fobjects-twitter%2F11033-scroll.png&f=1&nofb=1") // Set the author
                .WithColor(GetRainbowColor()) // Set the color
                .AddField("🚨 ARCHIVING MESSAGES STARTING NOW 🚨", "All messages sent from this point onwards will be archived and stored in a separate channel, possibly not in this server. To stop this archiving use the stoparchival command. This messsage has been pinned to help ensure its prominence.") // Add a (hopefully) attention-grabbing field
                .Build());

            try
            {
                // Attempt to pin the reply message
                await reply.PinAsync(new RequestOptions()
                {
                    AuditLogReason = "Reply to archive command"
                });
            }
            catch (Exception)
            {
                // If pin fails, send another message notifying of such
                await sourceChannel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithColor(GetRainbowColor())
                    .WithAuthor("Archival", "https://external-content.duckduckgo.com/iu/?u=https%3A%2F%2Fwww.emoji.co.uk%2Ffiles%2Ftwitter-emojis%2Fobjects-twitter%2F11033-scroll.png&f=1&nofb=1")
                    .AddField("__Error: Cannot Pin Message__", "Mafia Bot cannot pin this message. Potential causes of this include a lack of permissions, or having reached the maximum number of pins in this channel. It is heavily recommended that the above message be pinned manually.")
                    .Build());
                throw;
            }
        }

        /**
         * The StopArchive command.
         */
        [SlashCommand("stoparchive", "Sets the given channel to no longer be archived.")]
        public async Task StopArchiveAsync([Summary("source-channel", "The source channel to stop archiving, defaulting to this channel")] SocketTextChannel sourceChannel = default) => await StopArchiveAsync(sourceChannel == default ? null : sourceChannel.Id); // If referred to by channel, extract the ID and continue as usual

        [SlashCommand("stoparchive-id", "Sets the given channel to no longer be archived.")]
        public async Task StopArchiveAsync([Summary("source-channel-id", "The ID of the source channel to stop archiving, defaulting to this channel")] ulong? sourceChannelId = null)
        {
            // If sourceChannelId is not provided, default to the channel id of the contextual channel
            if (sourceChannelId == null) sourceChannelId = Context.Channel.Id;
            // Convert from nullable ulong to non-nullable ulong. Should never return, but just in case.
            if (sourceChannelId is not ulong sourceId) return;

            // Remove the channel from the JSON dictionary, so it is no longer archived
            await RemoveUlongFromJSONDictionaryAsync(sourceId, Config.ArchivalChannelsPath);

            // Convert the SocketChannel returned by GetChannel into a SocketTextChannel. If the channel is not a guild text channel, return.
            if (Context.Client.GetChannel(sourceId) is not SocketTextChannel sourceChannel)
            {
                await RespondAsync("Source channel must be a text channel.", ephemeral: true);
                return;
            }

            // Respond
            await RespondAsync("✅", ephemeral: true);
            
            // Send an embed in the source channel
            IUserMessage reply = await sourceChannel.SendMessageAsync(embed: new EmbedBuilder()
                .WithAuthor("Archival", "https://external-content.duckduckgo.com/iu/?u=https%3A%2F%2Fwww.emoji.co.uk%2Ffiles%2Ftwitter-emojis%2Fobjects-twitter%2F11033-scroll.png&f=1&nofb=1") // Set the author
                .WithColor(GetRainbowColor()) // Set the color
                .AddField("🚨 STOPPING ARCHIVAL 🚨", "Messages sent in this channel are no longer being archived. To resume archival use the archive command. This message has been pinned to help ensure its prominence.") // Add a (hopefully) attention-grabbing embed
                .Build());

            try
            {
                // Try to pin the message
                await reply.PinAsync(new RequestOptions()
                {
                    AuditLogReason = "Reply to stoparchive command"
                });
            }
            catch (Exception)
            {
                // Send another embed notifying of the failed pin
                await sourceChannel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithColor(GetRainbowColor()) // Set the color
                    .WithAuthor("Archival", "https://external-content.duckduckgo.com/iu/?u=https%3A%2F%2Fwww.emoji.co.uk%2Ffiles%2Ftwitter-emojis%2Fobjects-twitter%2F11033-scroll.png&f=1&nofb=1") // Set the author
                    .AddField("__Error: Cannot Pin Message__", "Mafia Bot cannot pin this message. Potential causes of this include a lack of permissions, or having reached the maximum number of pins in this channel. It is heavily recommended that the above message be pinned manually.") // Add a field describing that the pin failed
                    .Build());
            }
        }

        /**
         * The Purge command.
         */
        [SlashCommand("purge", "Instantly triggers channel purging. Can only be used by the bot's owner.")]
        [RequireOwner]
        public async Task TriggerPurgeAsync()
        {
            // Owner-only debug command that immediately triggers channel purging
            await PurgeChannelsAsync();
            // Respond with ✅
            await RespondAsync("✅", ephemeral: true);
        }

        /**
         * The Help command.
         */

        // Define an enum containing every slash command, for autocomplete
        public enum SlashCommands
        {
            all = -1,
            ping = 0,
            changestatus = 1,
            imagesonly = 2,
            resetpfp = 3,
            timedisplay = 4,
            pride = 5,
            pride_list = 6,
            translate = 7,
            archive = 8,
            archive_id = 9,
            stoparchive = 10,
            stoparchive_id = 11,
            purge = 12,
            help = 13,
            status = 14
        }

        [SlashCommand("help", "Lists the bots different commands.")]
        public async Task ListCommandsAsync([Summary("command", "A command to get more detail on")] SlashCommands command = SlashCommands.all)
        {
            // Get the commands that can be executed in the current context
            ICollection<SlashCommandInfo> commands = (ICollection<SlashCommandInfo>)_interactions.SlashCommands;

            // If a specific command has not been provided, provide a brief description of every command the bot has
            if (command == SlashCommands.all)
            {
                // Create an empty string to hold all of the commands and their syntaxes and summaries
                string helpMessage = "";
                // For each command the bot has
                foreach (SlashCommandInfo testCommand in commands)
                {
                    // Add two newlines, then add the command's name (alongside some bold formatting and the command prefix)
                    helpMessage += $"\n\n**/{testCommand.Name}**";
                    // For each of the command's parameters
                    foreach (SlashCommandParameterInfo parameter in testCommand.Parameters)
                    {
                        // If the parameter is optional
                        helpMessage += !parameter.IsRequired
                            ? $" **[{parameter.Name}]**"  // Add the parameter's name surrounded by square brackets (alongside some bold formatting)
                            : $" **<{parameter.Name}>**"; // Add the parameter's name surrounded by arrow brackets (alongside some bold formatting)
                    }
                    // Add a newline, and then the command's summary 
                    helpMessage += $"\n{testCommand.Description}";
                }

                // Respond with the embed
                await RespondAsync(embed: new EmbedBuilder()
                    .WithColor(GetRainbowColor())
                    .WithDescription(helpMessage)
                    .Build(), ephemeral: true);
            }
            else
            {
                // Create a new embed
                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(GetRainbowColor()); // Set the color

                // For each command 
                foreach (SlashCommandInfo testCommand in commands)
                {
                    // If none of the command's names match the provided command's name, skip this command
                    if (testCommand.Name != command.ToString().Replace('_', '-')) continue;
                    // Create a string to hold the command's syntax
                    string syntax = "";

                    // Add the command's name
                    syntax += $"/{testCommand.Name}";

                    // For each of the command's parameters
                    foreach (SlashCommandParameterInfo parameter in testCommand.Parameters)
                    {
                        // If the parameter is optional
                        syntax += !parameter.IsRequired 
                            ? $" [{parameter.Name}]"  // Add the parameter's name surrounded by square brackets
                            : $" <{parameter.Name}>"; // Add the parameter's name surrounded by arrow brackets
                    }
                    // Add a field to the embed, with the command syntax as the name of the field and the command's summary as the value
                    embed.AddField(syntax, testCommand.Description);

                    // For each of the command's parameters (again)
                    foreach (SlashCommandParameterInfo parameter in testCommand.Parameters)
                    {
                        // Add an inline field with the parameter's name and summary
                        embed.AddField(parameter.Name, parameter.Description, true);
                    }
                }

                // Respond with the embed
                await RespondAsync(embed: embed.Build(), ephemeral: true);
            }
        }

        /**
         * The Status command
         */
        [UserCommand("Display Status")]
        [SlashCommand("status", "Displays another user's current custom status")]
        public async Task DisplayStatusAsync([Summary("user", "The user whose status to display")] IUser user)
        {
            // Execute asynchronously
            await Task.Run(async () =>
            {
                // Get the user's custom status
                CustomStatusGame activity = await GetCustomStatusAsync(user);

                if (activity == default) // If the user has no custom status
                {
                    // Respond with a couple emojis to indicate that the bot couldn't find that user's custom status
                    await RespondAsync("❓🗒");
                    return; // Return
                }

                // Respond with an embed
                await RespondAsync(embed: new EmbedBuilder()
                    .WithColor(GetRainbowColor()) // Set the color
                    .WithAuthor(user) // Set the author to be the user
                    .WithTimestamp(activity.CreatedAt) // Set the timestamp to when the activity was created
                    .WithDescription($"{activity.Emote} {activity.State}") // Set the embed description to the activity
                    .Build());
            });
        }
    }
}
