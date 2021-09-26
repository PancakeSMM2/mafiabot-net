using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Translation.V2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Mafiabot.Functions;
using static Mafiabot.Program;

namespace Mafiabot
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        /** 
         * The Ping command.
        */
        [Command("ping")]
        [Summary("Replies with 🏓.")]
        public async Task PingAsync([Summary("Whether the bot should respond with the delay of the ping or not. If set to \"verbose\" or \"delay\", yes.")] string verbosity = "silent")
        {
            // If the specified verbosity is "verbose" or "delay"
            if (verbosity.ToLower() == "verbose" || verbosity.ToLower() == "delay")
            {
                // Store when the command message was sent, in milliseconds
                long commandSentMs = Context.Message.Timestamp.ToUnixTimeMilliseconds();
                // Calculate the delay between when the command was sent and now
                long delay = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - commandSentMs;

                // Reply to the message with the number of milliseconds of delay, and then a ping-pong paddle emoji
                await ReplyAsync($"{delay} ms delay 🏓");
            }
            else
            {
                // Otherwise, just react with 🏓
                await Context.Message.AddReactionAsync(new Emoji("🏓"));
            }
        }

        /** 
         * The ChangeStatus command.
         * To-do:
         * - Add a way to reset the bot's status
         */
        [Command("changestatus")]
        [Summary("Changes the bot's status.")]
        [Alias("status", "statuschange", "activity", "changeactivity", "setactivity", "setstatus")]
        [Priority(1)]
        public async Task ChangeStatusAsync([Summary("The status type to set. One of the following: PLAYING, STREAMING, LISTENING, WATCHING")] string activityType, [Remainder][Summary("The status text to set.")] string newActivity)
        {
            // Create an array to store the emotes to react with
            IEmote[] emotes = new IEmote[2];
            emotes[0] = new Emoji("✅");

            // Switch statement, holding each of the different activity types
            switch (activityType.ToUpper())
            {
                case "PLAYING": // ActivityType.Playing and 🎮
                    // Set the activity (of type Playing) to the supplied string
                    await Context.Client.SetActivityAsync(new Game(newActivity, ActivityType.Playing));
                    // Set the second reaction emoji to be 🎮, indicating the activity type
                    emotes[1] = new Emoji("🎮");
                    break;
                case "STREAMING": // ActivityType.Streaming and 🎥
                    // Set the activity (of type Streaming) to the supplied string
                    await Context.Client.SetActivityAsync(new Game(newActivity, ActivityType.Streaming));
                    // Set the second reaction emoji to be 🎥, indicating the activity type
                    emotes[1] = new Emoji("🎥");
                    break;
                case "LISTENING": // ActivityType.Listening and 🎧
                    // Set the activity (of type Listening) to the supplied string
                    await Context.Client.SetActivityAsync(new Game(newActivity, ActivityType.Listening));
                    // Set the second reaction emoji to be 🎧, indicating the activity type
                    emotes[1] = new Emoji("🎧");
                    break;
                case "WATCHING": // ActivityType.Watching and 📺
                    // Set the activity (of type Watching) to the supplied string
                    await Context.Client.SetActivityAsync(new Game(newActivity, ActivityType.Watching));
                    // Set the second reaction emoji to be 📺, indicating the activity type
                    emotes[1] = new Emoji("📺");
                    break;
                default:
                    // If no status type is specified, include the full message as the activity (and let the ActivityType default to Playing)
                    await Context.Client.SetActivityAsync(new Game(activityType + " " + newActivity));
                    // Set the second reaction emoji to be 🎮, indicating the activity type
                    emotes[1] = new Emoji("🎮");
                    break;
            }

            // React with the emotes
            await Context.Message.AddReactionsAsync(emotes);
        }

        // Overload for the ChangeStatus command, in case of a single-word status
        [Command("changestatus")]
        [Summary("Changes the bot's status, defaulting to type PLAYING")]
        [Alias("status", "statuschange", "activity", "changeactivity", "setactivity")]
        [Priority(0)]
        public async Task ChangeStatusAsync([Remainder][Summary("The status text to set.")] string newStatus)
        {
            // Set the activity to the supplied string
            await Context.Client.SetActivityAsync(new Game(newStatus));

            // React with ✅🎮 (indicating both that it was successful and that the status type has been defaulted to Playing)
            await Context.Message.AddReactionsAsync(new IEmote[]
            {
                new Emoji("✅"),
                new Emoji("🎮")
            });
        }

        /** 
         * The ImagesOnly command.
         */
        [Command("imagesonly")]
        [Summary("Sets a channel to be images only.")]
        [Alias("onlyimages")]
        [RequireContext(ContextType.Guild)]
        public async Task ImagesOnlyAsync()
        {
            // Execute asynchronously
            await Task.Run(async () =>
            {
                // Toggle the channel the command was sent from in imagesOnly.json, and store whether it was added or removed
                bool added = await ToggleUlongFromJSONAsync(Context.Channel.Id, Config.ImagesOnlyPath);

                // React with the emotes, ✅ if the channel is now images only, 🚫 if the channel is no longer images only, followed by 🖼
                await Context.Message.AddReactionsAsync(new IEmote[]
                {
                    added ? new Emoji("✅") : new Emoji("🚫"), // Ternary, depending on the added bool
                    new Emoji("🖼")
                });
            });
        }

        /** 
         * The ChangePfp command.
         */
        [Command("changepfp")]
        [Summary("Changes the bot's profile picture to an attached image.")]
        [Alias("pfp", "changeavatar", "avatar", "pfpchange", "avatarchange")]
        public async Task ChangePfpAsync()
        {
            // Execute asynchronously
            await Task.Run(async () =>
            {
                string url; // Declare an empty variable to hold the attachment's URL
                // Try-catch block to try and get the URL
                try
                {
                    // Set the URL to be the URL of the first attachment that has a designated width (which only images have)
                    url = Context.Message.Attachments.First((Attachment attached) => attached.Width != null).Url;
                }
                catch
                {
                    // If that fails (due to not finding a valid attachment), react with 🚫📸 to indicate that it did not find an image
                    await Context.Message.AddReactionsAsync(new IEmote[]
                    {
                        new Emoji("❓"),
                        new Emoji("📸")
                    });
                    // Then return
                    return;
                }

                // Attempt to change the bot's avatar, and store whether it was successful
                bool successful = await ChangeAvatarAsync(url, Context.Client.CurrentUser);

                if (successful) await Context.Message.AddReactionAsync(new Emoji("✅")); // If successful, react with ✅ (indicating as such)
                else await Context.Message.AddReactionsAsync(new IEmote[] // If unsuccessful, react with 🚫⏲️ (indicating that avatar changes are currently on cooldown)
                {
                    new Emoji("🚫"),
                    new Emoji("⏲️")
                });
            });
        }

        /** 
         * The ResetPfp command.
         */
        [Command("resetpfp")]
        [Summary("Resets the bot's profile picture to the default.")]
        [Alias("pfpreset", "resetavatar", "avatarreset", "avatareset")]
        public async Task ResetPfpAsync()
        {
            // Reset the avatar, and store whether it was successful
            bool successful = await ResetAvatarAsync(Context.Client.CurrentUser);

            if (successful) await Context.Message.AddReactionAsync(new Emoji("✅")); // If successful, react with ✅ (indicating as such)
            else await Context.Message.AddReactionsAsync(new IEmote[] // If unsuccessful, react with 🚫⏲️ (indicating that avatar changes are currently on cooldown)
            {
                    new Emoji("🚫"),
                    new Emoji("⏲️")
            });
        }

        /** 
         * The Reply command.
         */
        [Command("reply")]
        [Summary("Executes a provided command on the message this message is replying to.")]
        public async Task ExecuteReplyAsync([Summary("The command to execute on the reply, optionally alongside a prefix")][Remainder] string command)
        {
            if (Context.Message.ReferencedMessage is not SocketUserMessage reply) return; // If the referenced message isn't a user message, return

            // Check if the referenced message has an attached embed or file
            bool hasEmbed = reply.Embeds.FirstOrDefault() != default(Embed);
            bool hasFile = reply.Attachments.FirstOrDefault() != default(Attachment);

            // Declare two empty variables for use in the switch statement
            IUserMessage copy = null; // The variable to store the sent message in
            Stream data; // The variable to store a potential attachment's data in
            // Assemble the command message, with the override prefix and the provided command
            string builtCommand = $"[NO_BOT_OVERRIDE]{command} {reply.Content}";
            // Switch statement based on whether the message has an embed and whether it has a file
            switch (hasEmbed)
            {
                case true when hasFile: // Has an embed, and has a file
                    // Get the file's data
                    data = GetStreamFromImageUrl(reply.Attachments.First().Url);
                    // Send a (temporary) message mimicking as much of the referenced message as possible, including one of its embeds and one of its attachments, with the content being the built command
                    copy = await Context.Channel.SendFileAsync(data, reply.Attachments.First().Filename, builtCommand, false, reply.Embeds.First(), null, false, AllowedMentions.None, reply.Reference);
                    break;
                case true when !hasFile: // Has an embed, and has no file
                    // Send a (temporary) message mimicking as much of the referenced message as possible, including one of its embeds, with the content being the built command
                    copy = await Context.Channel.SendMessageAsync(builtCommand, false, reply.Embeds.First(), null, AllowedMentions.None, reply.Reference);
                    break;
                case false when hasFile: // Has no embed, and has a file
                    // Get the file's data
                    data = GetStreamFromImageUrl(reply.Attachments.First().Url);
                    // Send a (temporary) message mimicking as much of the referenced message as possible, including one of its attachments, with the content being the built command
                    copy = await Context.Channel.SendFileAsync(data, reply.Attachments.First().Filename, builtCommand, false, null, null, false, AllowedMentions.None, reply.Reference);
                    break;
                case false when !hasFile: // Has no embed, and has no file
                    // Send a (temporary) message mimicking as much of the referenced message as possible, with the content being the built command
                    copy = await Context.Channel.SendMessageAsync(builtCommand, false, null, null, AllowedMentions.None, reply.Reference);
                    break;
            }
            // Delete the temporary message
            await copy.DeleteAsync();
        }

        /**
         * The TimeDisplay command.
         */
        [Command("timedisplay")]
        [Summary("Displays the given time with Discord's timestamp formatting")]
        [Alias("displaytime", "time", "display", "timeconvert", "converttime", "convert")]
        public async Task TimeConvertAsync([Summary("The time to display, such as\"8:00 AM\" or \"May 8, 1988 5:49 PM\"")][Remainder] string time)
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
                // If the parse fails
                await Context.Message.AddReactionsAsync(new IEmote[]
                {
                    new Emoji("❓"),
                    new Emoji("📆")
                });
                // Return
                return;
            }

            // Reply with both conversions
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(GetRainbowColor()) // Set the color
                .AddField("Exact Time", $"<t:{parsed.ToUnixTimeSeconds()}:f>") // Add a field with the exact timestamp
                .AddField("Relative Time", $"<t:{parsed.ToUnixTimeSeconds()}:R>") // Add a field with the relative timestamp
                .WithTimestamp(parsed); // Set the timestamp
            await ReplyAsync(embed: embed.Build());
        }

        /**
         * The Pride command.
         */
        [Command("pride")]
        [Summary("Sends a specified pride flag")]
        [Alias("prideflag", "flag")]
        [Priority(1)]
        public async Task PrideFlagAsync([Summary("The flag to display")][Remainder] string flagName)
        {
            // Get the pride flag
            PrideFlag flag = await GetPrideFlagAsync(flagName);

            // Construct a reply embed
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(GetRainbowColor()) // Random rainbow color
                .WithImageUrl(flag.Url) // Display the flag
                .WithAuthor("Pride", "https://media.discordapp.net/attachments/716108846816297040/823648970215522304/image.png"); // Set the author

            // Reply with the embed
            await ReplyAsync(embed: embed.Build());
        }

        [Command("pride")]
        [Summary("Lists all supported pride flags and their aliases")]
        [Alias("prideflag", "flag")]
        [Priority(0)]
        public async Task PrideFlagAsync()
        {
            // Get the pride flag names
            string names = await GetPrideFlagNamesAsync();

            // Construct a reply embed
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(GetRainbowColor()) // Random rainbow color
                .WithAuthor("Pride", "https://media.discordapp.net/attachments/716108846816297040/823648970215522304/image.png") // Set the author
                .AddField("!pride <Flag>", "\u200b") // First field, with the command syntax
                .AddField("Flag", "One of the supported pride flags, as listed below. If you request a flag that isn't supported, you'll just get the progressive flag. If you want a flag added to this, lemme know!") // Second field, describing the Flag argument
                .AddField("SupportedFlags", names) // Third field, with the pride flag names
                .WithImageUrl("https://cdn.discordapp.com/attachments/716108846816297040/823668797248372766/Z.png"); // Send the progressive flag

            // Reply with the embed
            await ReplyAsync(embed: embed.Build());
        }

        /**
         * The Translate command.
         */
        // Create the translation client with default credentials
        private static readonly TranslationClient translate = TranslationClient.Create();

        [Command("translate")]
        [Summary("Repeatedly translates a given string, defaulting to 5 cycles")]
        [Priority(0)]
        public async Task TranslateAsync([Summary("The text to translate")][Remainder] string text)
        {
            // If cycles is not provided, default to 5
            await TranslateAsync(5, text);
        }

        [Command("translate")]
        [Summary("Repeatedly translates a given string")]
        [Priority(1)]
        public async Task TranslateAsync([Summary("The number of cycles to translate for, maximum of 50")] int cycles, [Summary("The text to translate")][Remainder] string text)
        {
            // Execute asynchronously
            await Task.Run(async () =>
            {
                // Sanity check for number of cycles
                if (cycles > 50)
                {
                    // Reply
                    await ReplyAsync(embed: new EmbedBuilder()
                        .WithAuthor("Translate", "https://cdn.discordapp.com/attachments/716108846816297040/823295348474642492/1024px-Google_Translate_logo.png")
                        .WithColor(GetRainbowColor())
                        .WithDescription("Cycles are limited at a maximum of 50\nNote that increased cycles have diminishing returns")
                        .Build());
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

                // Send the embed, and store the reply message
                Discord.Rest.RestUserMessage replyMessage = await Context.Message.Channel.SendMessageAsync(embed: embed.Build());

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

                    // Edit the reply message to show the new embed
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    replyMessage.ModifyAsync((properties) =>
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
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithAuthor("Translate", "https://cdn.discordapp.com/attachments/716108846816297040/823295348474642492/1024px-Google_Translate_logo.png") // Set the author
                    .WithColor(GetRainbowColor()) // Set the color
                    .AddField("Output", translateText) // Add a field with the final text
                    .Build());
            });
        }

        /**
         * The Archive command.
         */
        [Command("archive")]
        [Summary("Sets the given channel to be archived in the given channel")]
        public async Task MarkArchiveAsync([Summary("The channel to archive to")] IChannel targetChannel, [Summary("The channel to archive from, defaulting to the channel the command is being executed in")] IChannel sourceChannel = null)
        {
            // If there is no provided source channel, default to the contextual channel
            if (sourceChannel == null) sourceChannel = Context.Channel;

            // If both channels are referenced via a channel link, extract the id from it and execute as normal
            await MarkArchiveAsync(targetChannel.Id, sourceChannel.Id);
        }   

        [Command("archive")]
        [Summary("Sets the given channel to be archived in the given channel")]
        public async Task MarkArchiveAsync([Summary("The ID of the channel to archive to")] ulong targetChannelId, [Summary("The ID of the channel to archive from, defaulting to the channel the command is being executed in")] ulong? sourceChannelId = null)
        {
            // If there is no provided source channel id, default to the contextual channel id
            if (sourceChannelId == null) sourceChannelId = Context.Channel.Id;
            // Convert the nullable souceChannelId to the non-nullable sourceId. Should never return, but just in case.
            if (sourceChannelId is not ulong sourceId) return;

            // Assign the target channel to the source channel in archivalChannels.json
            await AssignUlongToJSONDictionaryAsync(sourceId, targetChannelId, Config.ArchivalChannelsPath);

            // Convert the SocketChannel returned by GetChannel and convert it to a SocketTextChannel. If it isn't a guild text channel, return.
            if (Context.Client.GetChannel(sourceId) is not SocketTextChannel sourceChannel) return;

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
        [Command("stoparchive")]
        [Summary("Sets the given channel to no longer be archived.")]
        public async Task StopArchiveAsync([Summary("The source channel to stop archiving")] IChannel sourceChannel) => await StopArchiveAsync(sourceChannel.Id); // If referred to by channel, extract the ID and continue as usual

        [Command("stoparchive")]
        [Summary("Sets the given channel to no longer be archived.")]
        public async Task StopArchiveAsync([Summary("The ID of the source channel to stop archiving, defaulting to the channel the command is executed in")] ulong? sourceChannelId = null)
        {
            // If sourceChannelId is not provided, default to the channel id of the contextual channel
            if (sourceChannelId == null) sourceChannelId = Context.Channel.Id;
            // Convert from nullable ulong to non-nullable ulong. Should never return, but just in case.
            if (sourceChannelId is not ulong sourceId) return;

            // Remove the channel from the JSON dictionary, so it is no longer archived
            await RemoveUlongFromJSONDictionaryAsync(sourceId, Config.ArchivalChannelsPath);

            // Convert the SocketChannel returned by GetChannel into a SocketTextChannel. If the channel is not a guild text channel, return.
            if (Context.Client.GetChannel(sourceId) is not SocketTextChannel sourceChannel) return;

            // Reply with an embed
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
        [Command("purge")]
        [Summary("Instantly triggers channel purging. Can only be used by the bot's owner, and is only to be used to make up for times that the bot failed to purge overnight.")]
        [RequireOwner]
        public async Task TriggerPurgeAsync()
        {
            // Owner-only debug command that immediately triggers channel purging
            await PurgeChannelsAsync();
            // React with ✅
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        /**
         * The Help command.
         */
        [Command("help")]
        [Summary("Lists the bots different commands.")]
        public async Task ListCommandsAsync([Summary("A command to get more detail on")] string command = default)
        {
            // Get the commands that can be executed in the current context
            IReadOnlyCollection<CommandInfo> commands = await Program._commands.GetExecutableCommandsAsync(Context, null);

            // If a specific command has not been provided, provide a brief description of every command the bot has
            if (command == default)
            {
                // Create an empty string to hold all of the commands and their syntaxes and summaries
                string helpMessage = "";
                // For each command the bot has
                foreach (CommandInfo testCommand in commands)
                {
                    // Add two newlines, then add the command's name (alongside some bold formatting and the command prefix)
                    helpMessage += $"\n\n**!{testCommand.Name}**";
                    // For each of the command's parameters
                    foreach (ParameterInfo parameter in testCommand.Parameters)
                    {
                        // If the parameter is optional
                        helpMessage += parameter.IsOptional
                            ? $" **[{parameter.Name}]**"  // Add the parameter's name surrounded by square brackets (alongside some bold formatting)
                            : $" **<{parameter.Name}>**"; // Add the parameter's name surrounded by arrow brackets (alongside some bold formatting)
                    }
                    // Add a newline, and then the command's summary 
                    helpMessage += $"\n{testCommand.Summary}";
                }

                // Reply with the embed
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetRainbowColor())
                    .WithDescription(helpMessage)
                    .Build());
            }
            else
            {
                // Create a new embed
                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(GetRainbowColor()); // Set the color

                // For each command 
                foreach (CommandInfo testCommand in commands)
                {
                    // If none of the command's names match the provided command's name, skip this command
                    if (!testCommand.Aliases.Any(alias => alias.ToLower() == command.ToLower())) continue;
                    // Create a string to hold the command's syntax
                    string syntax = "";

                    // Add the command's first alias
                    syntax += $"!{testCommand.Aliases[0]}";
                    // For each of the command's aliases
                    foreach (string alias in testCommand.Aliases)
                    {
                        // If the current alias is the first alias, that alias has already been added and thus it should be skipped now
                        if (alias == testCommand.Aliases[0]) continue;
                        // Otherwise, add the alias to the syntax string
                        syntax += $" | {alias}";
                    }

                    // For each of the command's parameters
                    foreach (ParameterInfo parameter in testCommand.Parameters)
                    {
                        // If the parameter is optional
                        syntax += parameter.IsOptional 
                            ? $" [{parameter.Name}]"  // Add the parameter's name surrounded by square brackets
                            : $" <{parameter.Name}>"; // Add the parameter's name surrounded by arrow brackets
                    }
                    // Add a field to the embed, with the command syntax as the name of the field and the command's summary as the value
                    embed.AddField(syntax, testCommand.Summary);

                    // For each of the command's parameters (again)
                    foreach (ParameterInfo parameter in testCommand.Parameters)
                    {
                        // Add an inline field with the parameter's name and summary
                        embed.AddField(parameter.Name, parameter.Summary, true);
                    }
                }

                // Reply with the embed
                await ReplyAsync(embed: embed.Build());
            }
        }
    }
}
