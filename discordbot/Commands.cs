using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using static mafiabot.Functions;

namespace mafiabot
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        /** 
         The Ping command.
         In future, I desire to add a millisecond delay display
         */
        [Command("ping")]
        [Summary("Replies with \"Pong!\".")]
        public async Task PingAsync()
        {
            long receivedAt = Context.Message.Timestamp.ToUnixTimeMilliseconds();
            await ReplyAsync("Pong!").ContinueWith((Task<IUserMessage> task) =>
            {
                long delay = task.Result.Timestamp.ToUnixTimeMilliseconds() - receivedAt;
                task.Result.ModifyAsync((MessageProperties properties) =>
                {
                    properties.Content = $"Pong! {delay}s delay";
                });
            });

        }

        [Command("changestatus")]
        [Summary("Changes the bot's status.")]
        [Alias("status", "statuschange", "activity", "changeactivity", "setactivity")]
        [Priority(1)]
        public async Task ChangeStatusAsync([Summary("The status type to set. One of the following: PLAYING STREAMING LISTENING WATCHING COMPETING")] string statusType, [Remainder] [Summary("The status text to set.")] string newStatus)
        {
            IEmote[] emotes = new IEmote[2];

            switch(statusType.ToUpper()) {
                case "PLAYING":
                    await Context.Client.SetActivityAsync(new Game(newStatus, ActivityType.Playing));
                    emotes[0] = new Emoji("✅");
                    emotes[1] = new Emoji("🎮");
                    break;
                case "STREAMING":
                    await Context.Client.SetActivityAsync(new Game(newStatus, ActivityType.Streaming));
                    emotes[0] = new Emoji("✅");
                    emotes[1] = new Emoji("🎥");
                    break;
                case "LISTENING":
                    await Context.Client.SetActivityAsync(new Game(newStatus, ActivityType.Listening));
                    emotes[0] = new Emoji("✅");
                    emotes[1] = new Emoji("🎧");
                    break;
                case "WATCHING":
                    await Context.Client.SetActivityAsync(new Game(newStatus, ActivityType.Watching));
                    emotes[0] = new Emoji("✅");
                    emotes[1] = new Emoji("📺");
                    break;
                default:
                    await Context.Client.SetActivityAsync(new Game(statusType + " " + newStatus));
                    emotes[0] = new Emoji("✅");
                    emotes[1] = new Emoji("🎮");
                    break;
            }

            await Context.Message.AddReactionsAsync(emotes);
        }

        [Command("changestatus")]
        [Summary("Changes the bot's status.")]
        [Alias("status", "statuschange", "activity", "changeactivity", "setactivity")]
        [Priority(0)]
        public async Task ChangeStatusAsync([Remainder][Summary("The status text to set.")] string newStatus)
        {
            await Context.Client.SetActivityAsync(new Game(newStatus));
            IEmote[] emotes = new IEmote[2];
            emotes[0] = new Emoji("✅");
            emotes[1] = new Emoji("🎮");
            await Context.Message.AddReactionsAsync(emotes);
        }

        [Command("imagesonly")]
        [Summary("Sets a channel to be images only.")]
        public async Task ImagesOnlyAsync()
        {
            await Task.Run(async () =>
            {
                bool removed = await ToggleSnowflakeFromJSONAsync(Context.Channel.Id, "./imagesonly.json");
                IEmote[] emotes = new IEmote[2];
                if (removed) emotes[0] = new Emoji("✅");
                else emotes[0] = new Emoji("🚫");
                emotes[1] = new Emoji("💬");
                await Context.Message.AddReactionsAsync(emotes);
            });
        }
    }
}
