using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Mafiabot.Program;

namespace Mafiabot
{
    internal static class Functions
    {
        // Toggles an ulong from an ulong array stored in a JSON file (or possibly another file, just as long as it can be read by the File.ReadAllText function and is formatted like JSON)
        public static async Task<bool> ToggleUlongFromJSONAsync(ulong toggle, string filePath)
        {
            // Execute asynchronously
            return await Task.Run(async () =>
            {
                // Get the ulong array from the file
                ulong[] output = await GetUlongsFromJSONAsync(filePath);
                // Get whether the ulong is contained within the ulong array
                bool has = output.Contains(toggle);

                // If the ulong array has the ulong, it should be removed
                if (has)
                {
                    // Get the index of the ulong in the ulong array
                    int index = Array.IndexOf(output, toggle);
                    // Create a new ulong array as long as the output ulong array
                    ulong[] replacement = new ulong[output.Length];
                    // Create a boolean to hold whether the toggled ulong has been removed
                    bool pastRemoval = false;

                    // Effectively a foreach loop, except with access to i
                    for (int i = 0; i < output.Length; i++)
                    {
                        if (i != index) // If the currently tested ulong is not the ulong to be deleted
                            replacement[i] = pastRemoval  // This index in the replacement ulong array, if it has gone past the ulong to be removed already
                            ? output[i - 1] // Ensures every ulong is shifted backwards one index, to account for the toggled ulong being removed
                            : output[i]; // If this is before the removal, just copy from the old array to the new one
                        // If the currently tested ulong is the ulong to be deleted, do not add it to the replacement array and set that it's past the removal
                        else pastRemoval = true;
                    }

                    // Replace the first ulong array with the replacement ulong array
                    output = replacement;
                }
                else // If the ulong array does not have the ulong, it should be added
                {
                    // Create a new ulong array to replace the old one, as long as the previous one + 1
                    ulong[] replacement = new ulong[output.Length + 1];
                    // Effectively a foreach loop, except with access to i
                    for (int i = 0; i < output.Length; i++)
                    {
                        // Copy all of the values of output to replacement
                        // Not just doing replacement = output in order to preserve the length of replacement
                        replacement[i] = output[i];
                    }
                    // Set the last value of replacement to be the toggled ulong
                    replacement[output.Length] = toggle;
                    // Replace the output with replacement
                    output = replacement;
                }
                // Serialize output to a JSON string
                string newContent = JsonConvert.SerializeObject(output);
                // Write that to the file
                File.WriteAllText(filePath, newContent);

                // Return the inverse of has (as now whichever value it had before has been inverted)
                return !has;
            });
        }

        // Returns whether a certain file (formatted like JSON) contains a given ulong
        public static async Task<bool> CheckUlongFromJSONAsync(ulong check, string filePath)
        {
            // Execute asynchronously
            return await Task.Run(async () =>
            {
                // Get the ulong array from the file
                ulong[] output = await GetUlongsFromJSONAsync(filePath);

                // Return whether the ulong array contains the check ulong
                return output.Contains(check);
            });
        }

        // Returns an ulong array from a given file path
        public static async Task<ulong[]> GetUlongsFromJSONAsync(string filePath)
        {
            // Execute asynchronously
            return await Task.Run(() =>
            {
                // Read all of the text from the given file path
                string content = File.ReadAllText(filePath);
                // Return that text deserialized into an ulong array
                return JsonConvert.DeserializeObject<ulong[]>(content);
            });
        }

        // Adds a given key-value ulong pair to a dictionary stored in JSON format in a file
        public static async Task AssignUlongToJSONDictionaryAsync(ulong key, ulong value, string filePath)
        {
            // Execute asynchronously
            await Task.Run(() =>
            {
                // Read all of the text from the file
                string content = File.ReadAllText(filePath);
                // Deserialize the text to a ulong key-ulong value dictionary
                Dictionary<ulong, ulong> output = JsonConvert.DeserializeObject<Dictionary<ulong, ulong>>(content);

                // Add the key-value pair to the dictionary
                output[key] = value;

                // Serialize it back into a JSON string
                string newContent = JsonConvert.SerializeObject(output);
                // Write back to the file
                File.WriteAllText(filePath, newContent);
            });
        }

        // Removes a given key-value pair from a dictionary stored in a JSON-formatted file
        public static async Task RemoveUlongFromJSONDictionaryAsync(ulong key, string filePath)
        {
            // Execute asynchronously
            await Task.Run(() =>
            {
                // Read all of the file's text
                string content = File.ReadAllText(filePath);
                // Deserialize it to an ulong key-ulong value dictionary
                Dictionary<ulong, ulong> output = JsonConvert.DeserializeObject<Dictionary<ulong, ulong>>(content);

                // Remove the key-value pair, referred to by the key
                output.Remove(key);

                // Serialize it back to a JSON-formatted string
                string newContent = JsonConvert.SerializeObject(output);
                // Write the file
                File.WriteAllText(filePath, newContent);
            });
        }

        // Returns the value of a given key-value pair
        public static async Task<ulong> GetChannelFromJSONDictionaryAsync(ulong keyChannelId, string filePath)
        {
            // Execute asynchronously
            return await Task.Run(() =>
            {
                // Read the file's text
                string content = File.ReadAllText(filePath);
                // Deserialize to a ulong key-ulong value dictionary
                Dictionary<ulong, ulong> output = JsonConvert.DeserializeObject<Dictionary<ulong, ulong>>(content);

                // If the dictionary has that value
                if (output.TryGetValue(keyChannelId, out ulong value))
                {
                    // Return it
                    return value;
                }
                else return default; // Otherwise, return default
            });
        }

        // Defines an enum to use for whenever a string is used to refer to an image
        public enum ImageReferenceMethod
        {
            Url = 0, // If the string is a URL
            FilePath = 1 // If the string is a file path
        }
        // Defines a DateTimeOffset to use to store the next time it'll be safe to change the bot's profile picture
        private static DateTimeOffset nextAvatarChange = DateTimeOffset.UtcNow.AddMinutes(10);
        // Changes the provided user's avatar (in 99% of cases the bot) to the provided image
        public static async Task<bool> ChangeAvatarAsync(string reference, SocketSelfUser user, ImageReferenceMethod method = ImageReferenceMethod.Url, bool overrideTimer = false)
        {
            // If it's too early to change the avatar again, return a failure
            if (!(nextAvatarChange < DateTimeOffset.UtcNow || overrideTimer || Config.Development)) // If overrideTimer is true, skip this check. Do the same if in development mode
            {
                return false;
            }

            // Get the image data in a stream, depending on the ImageReferenceMethod
            Stream imageData = method == ImageReferenceMethod.Url // If the method is via URL
                ? GetStreamFromImageUrl(reference) // Get the image data via URL
                : File.OpenRead(reference);   // Otherwise, use the reference as a file path and read that file

            // Create a new image using the image data
            Image newAvatar = new(imageData);

            // Try-catch block, in case (despite my precautions) setting the avatar still fails
            try
            {
                // Change the user's avatar
                await user.ModifyAsync((SelfUserProperties properties) =>
                {
                    // Set the user's avatar to the new avatat
                    properties.Avatar = newAvatar;
                });
                // Reset the avatar change cooldown
                nextAvatarChange = DateTimeOffset.UtcNow.AddMinutes(10);

                // Return a success
                return true;
            }
            catch (Exception)
            {
                // If the avatar change failed, return a failure
                return false;
            }
        }

        // Returns a stream downloaded from a provided image URL
        public static Stream GetStreamFromImageUrl(string url)
        {
            // Defines an array of bytes to hold the image data
            byte[] imageData = null;

            // Create a webclient to use to download the image data
            using (System.Net.WebClient wc = new())
                // Set the image data to the downloaded data from the URL
                imageData = wc.DownloadData(url);

            // Return a new stream made from the image data
            return new MemoryStream(imageData);
        }

        // Resets the avatar of a provided user (the bot, in 99% of cases)
        public static async Task<bool> ResetAvatarAsync(SocketSelfUser user, bool overrideTimer = false)
        {
            // Execute asynchronously
            return await Task.Run(async () =>
            {
                // Get the default avatar path
                string path = Config.DefaultAvatarPath;
                // Return the result of changing the avatar to the default avatar
                return await ChangeAvatarAsync(path, user, ImageReferenceMethod.FilePath, overrideTimer);
            });
        }

        // Defines a class to hold pride flag data
        public class PrideFlag
        {
            public string Url
            { get; set; }
            public string[] Names
            { get; set; }
        }

        // Returns all of the pride flags
        public static async Task<PrideFlag[]> GetPrideFlagsAsync()
        {
            // Execute asynchronously
            return await Task.Run(() =>
            {
                // Get the content from prideFlags.json
                string content = File.ReadAllText(Config.PrideFlagsPath);
                // Deserialize the content to a pride flag array
                PrideFlag[] flags = JsonConvert.DeserializeObject<PrideFlag[]>(content);
                // Return that array
                return flags;
            });
        }

        // Get a specific pride flag, requested by name
        public static async Task<PrideFlag> GetPrideFlagAsync(string flagName)
        {
            // Execute asynchronously
            return await Task.Run(async () =>
            {
                // Get the pride flags
                PrideFlag[] flags = await GetPrideFlagsAsync();

                // Get the first flag where any of its names match the requested flag name
                PrideFlag flag = flags.FirstOrDefault(test =>
                {
                    // Return whether any of the flag's names match the requested name (caps-insensitive)
                    return test.Names.FirstOrDefault(name => name.ToLower() == flagName.ToLower()) != default;
                });

                // If the flag was found
                if (flag != default)
                {
                    // Return the flag
                    return flag;
                }
                else return new PrideFlag // Otherwise, default to returning the progressive flag
                {
                    Url = "https://cdn.discordapp.com/attachments/716108846816297040/823668797248372766/Z.png",
                    Names = new string[] { "Progressive" }
                };
            });
        }

        // Returns a string containing all of the pride flag names
        public static async Task<string> GetPrideFlagNamesAsync()
        {
            // Execute asynchronously
            return await Task.Run(async () =>
            {
                // Get the pride flags
                PrideFlag[] flags = await GetPrideFlagsAsync();

                // Create a string to hold all of the flag names
                string names = "";

                // Effectively a foreach, but with access to i
                for (int i = 0; i < flags.Length; i++)
                {
                    // Effectively a foreach, but with access to i
                    for (int index = 0; index < flags[i].Names.Length; index++)
                    {
                        // Extracts the current name
                        string name = flags[i].Names[index];
                        // If this is the first name of this flag
                        if (index == 0)
                        {
                            // Add a newline, and then the flag's name
                            names += $"\n{name}";
                        }
                        else
                        {
                            // Otherwise, don't add a newline, and add a slash and the command's name
                            names += $" / {name}";
                        }
                    }
                }

                // Return the string of names
                return names;
            });
        }

        // Create a new random generator
        private static readonly Random random = new();

        // Returns a random integer between 0 (inclusive) and the provided maximum (exclusive)
        public static int RandomInt(int exclusiveMaximum)
        {
            // Perform the other RandomInt function with the minimum as 0
            return RandomInt(0, exclusiveMaximum);
        }

        // Return a random integer between the provided minimum (inclusive) and the provided maximum (exclusive)
        public static int RandomInt(int inclusiveMinimum, int exclusiveMaximum)
        {
            // Randomly generate a number between the minimum and maximum
            return random.Next(inclusiveMinimum, exclusiveMaximum);
        }

        // Defines a static color array to hold all of the colors of the rainbow
        private static readonly Color[] rainbowColors = new Color[]
            {
                new Color(0xe60000), // Red
                new Color(0xd98d00), // Orange
                new Color(0xffff00), // Yellow
                new Color(0x00e308), // Green
                new Color(0x0057d1), // Blue
                new Color(0x5f02e0), // Indigo
                new Color(0x9200d6)  // Violet
            };

        // Returns a random rainbow color
        // Potential to-do, have this randomly generate a new color of a random hue, rather than just returning one of 7 preset rainbow colors
        public static Color GetRainbowColor()
        {
            // Get a random int with its maximum value being the length of rainbowColors
            int randomInt = RandomInt(rainbowColors.Length);
            // Call the other GetRainbowColor function with the generated random int
            return GetRainbowColor(randomInt);
        }

        // Returns a requested rainbow color
        public static Color GetRainbowColor(int roygbiv) // *R*ed *O*range *Y*ellow *G*reen *B*lue *I*ndigo *V*iolet
        {
            // If roygbiv is too high, round it down
            if (roygbiv >= rainbowColors.Length) roygbiv = rainbowColors.Length - 1;
            // If roygbiv is too low, round it up
            if (roygbiv < 0) roygbiv = 0;

            // Return the color
            return rainbowColors[roygbiv];
        }

        // Purges all channels stored in purgeChannels.json
        public static async Task PurgeChannelsAsync()
        {
            // Execute asynchronously
            await Task.Run(async () =>
            {
                // Get the purge channel IDs
                ulong[] purgeChannels = await GetUlongsFromJSONAsync("./purgeChannels.json");

                // For each of the IDs
                foreach (ulong channelId in purgeChannels)
                {
                    // Get that channel
                    SocketChannel channel = _client.GetChannel(channelId);
                    // If that channel is not a guild text channel, skip that channel
                    if (channel is not SocketTextChannel textChannel) continue;

                    // A boolean to store whether the channel still has (deletable) messages left
                    bool hasMessages;
                    // Define an IMessage variable to store the oldest message found in the channel
                    IMessage oldestMessage = default;
                    // Do-while loop to repeat the process of fetching messages and deleting them until there are no deletable messages left
                    do
                    {
                        // Get a list of messages from the channel, max of 100
                        List<IReadOnlyCollection<IMessage>> messages = oldestMessage == default // If the oldest message hasn't been assigned yet
                            ? await textChannel.GetMessagesAsync().ToListAsync() // Get the channel's messages
                            : await textChannel.GetMessagesAsync(oldestMessage, Direction.Before).ToListAsync(); // Otherwise, only get messages sent before the oldest message
                        // Update hasMessages
                        hasMessages = messages[0].Count != 0;

                        // If the channel has messages left in it
                        if (hasMessages)
                        {
                            // Create a new IEnumerable to contain all of the messages
                            IEnumerable<IMessage> combinedMessages = Array.Empty<IMessage>();
                            // For each of the individual collections returned by GetMessagesAsync
                            foreach (IReadOnlyCollection<IMessage> someMessages in messages)
                            {
                                // Combine them with all of hte other collections
                                combinedMessages = combinedMessages.Concat(someMessages);
                            }

                            // Set oldestMessage to be the last message in combinedMessages
                            oldestMessage = combinedMessages.Last();

                            // Create another IEnumerable to contain all of the messages that can be deleted (Bots cannot delete messages sent more than 2 weeks ago)
                            IEnumerable<IMessage> deletableMessages = Array.Empty<IMessage>();
                            // For each of the messages
                            foreach (IMessage message in combinedMessages)
                            {
                                // Get an int storing whether the message was sent more than 2 weeks ago (1) exactly 2 weeks ago (0) or less than 2 weeks ago (-1)
                                int deletable = DateTimeOffset.UtcNow.AddDays(-14).CompareTo(message.CreatedAt);

                                // If the message was sent less than 2 weeks ago
                                if (deletable == -1)
                                {
                                    // Add it to deletableMessages
                                    deletableMessages = deletableMessages.Append(message);
                                }
                            }

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                            // If there are any deletable messages, delete them
                            if (deletableMessages.Any()) textChannel.DeleteMessagesAsync(deletableMessages, new RequestOptions()
                            {
                                AuditLogReason = "Routine purge, performed by Mafia Bot. To disable this, remove the channel's ID from purgeChannels.json" // With an audit log reason
                            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                            // If there are no deletable messages, set hasMessages to false
                            else hasMessages = false;
                        }
                    } while (hasMessages); // Repeat until the channel has no deletable messages
                }
            });
        }

        // Gets a user's current custom status
        public static async Task<CustomStatusGame> GetCustomStatusAsync(IUser user)
        {
            // Execute asynchronously
            return await Task.Run(() =>
            {
                // Return the user's custom status activity
                return (CustomStatusGame)user.Activities.FirstOrDefault((activity) => activity.Type == ActivityType.CustomStatus);
            });
        }

        public static async Task<FileStream> GetEmojiImageAsync(string fileName)
        {
            return await Task.Run(() =>
            {
                FileStream image = File.OpenRead($"{Config.EmojiImageFolderPath}{fileName}");
                return image;
            });
        }
    }
}
