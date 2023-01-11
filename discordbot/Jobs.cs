using Discord;
using Quartz;
using System.Threading.Tasks;
using static Mafiabot.Functions;

namespace Mafiabot
{
    namespace Jobs
    {
        // Used to routinely reset the bot's avatar
        public class AvatarResetJob : IJob
        {
            // When the job is executed
            public async Task Execute(IJobExecutionContext context)
            {
                // Log that the job has been triggered
                await Program.LogAsync(new LogMessage(LogSeverity.Info, "Mafiabot", "AvatarResetJob has been triggered"));
                // Reset the bot's avatar
                await ResetAvatarAsync(Program._client.CurrentUser, true);
            }
        }

        // Used to routinely update ongoing posts
        public class PostUpdateJob : IJob
        {
            // When the job is executed
            public async Task Execute(IJobExecutionContext context)
            {
                // Log that the job has been triggered
                await Program.LogAsync(new LogMessage(LogSeverity.Info, "Mafiabot", "PostUpdateJob has been triggered"));
                // Update the posts
                await Program._posts.UpdatePostsAsync();
            }
        }

        // Used to routinely purge the purge channels
        public class ChannelPurgeJob : IJob
        {
            // When the job is executed
            public async Task Execute(IJobExecutionContext context)
            {
                // Log that the job has been triggered
                await Program.LogAsync(new LogMessage(LogSeverity.Info, "Mafiabot", "ChannelPurgeJob has been triggered"));
                // Purge
                await PurgeChannelsAsync();
            }
        }
    }
}
