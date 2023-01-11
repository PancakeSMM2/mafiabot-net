using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mafiabot.Posts
{
    /// <summary>
    /// Manages and saves the list of posts.
    /// </summary>
    internal class PostService
    {
        /// <summary>
        /// The file path of the JSON file to save and load the posts from
        /// </summary>
        private string FilePath { get; set; }
        private DiscordSocketClient Client { get; }
        /// <summary>
        /// The read-only dictionary of posts stored by this PostService.
        /// </summary>
        public ImmutableDictionary<string, Post> Posts { get; private set; } = ImmutableDictionary<string, Post>.Empty;

        /// <summary>
        /// Creates a new PostService with a given file path.
        /// </summary>
        /// <param name="filePath">The file path of the JSON file to store and load the posts from.</param>
        /// <param name="client">The client for this PostService to access.</param>
        public PostService(string filePath, DiscordSocketClient client)
        {
            // Store the file path
            FilePath = filePath;
            
            // Load any saved posts, and then store them
            ImmutableDictionary<string, Post> posts = JsonConvert.DeserializeObject<ImmutableDictionary<string, Post>>(File.ReadAllText(FilePath));
            Posts = posts ?? ImmutableDictionary<string, Post>.Empty; // If the posts failed to load, replace them with an empty dictionary

            // Store the client
            Client = client;
        }

        /// <summary>
        /// Sets up the PostService.
        /// </summary>
        public void InstallPosts()
        {
            // Hook into the Client.Ready event
            Client.Ready += OnClientReadyAsync;
        }

        /// <summary>
        /// Called on Client.Ready to set up the activity.
        /// </summary>
        /// <returns></returns>
        private async Task OnClientReadyAsync()
        {
            // Update the bot's activity to display the posts
            await UpdateActivityAsync();
        }

        /// <summary>
        /// Saves a provided post.
        /// </summary>
        /// <param name="post">The post to save.</param>
        /// <returns></returns>
        public async Task SavePostAsync(Post post)
        {
            // Set the new post in the dictionary
            Posts = Posts.SetItem(post.Name, post); // The name is used as a key, so that posts with identical names overwrite each other

            // Display the new post
            await UpdateActivityAsync();
            // Update the JSON file
            await UpdateJsonAsync();
        }

        /// <summary>
        /// Deletes a provided post.
        /// </summary>
        /// <param name="name">The name of the post to delete.</param>
        /// <returns></returns>
        public async Task DeletePostAsync(string name)
        {
            // Remove the provided post from the dictionary
            Posts = Posts.Remove(name);

            // Stop displaying the post
            await UpdateActivityAsync();
            // Update the JSON file
            await UpdateJsonAsync();
        }

        /// <summary>
        /// Updates the JSON file with the current state of the Posts dictionary
        /// </summary>
        /// <returns></returns>
        private async Task UpdateJsonAsync()
        {
            // Serialize the posts to JSON
            string newText = JsonConvert.SerializeObject(Posts);

            // Save that JSON to the file
            await File.WriteAllTextAsync(FilePath, newText);
        }

        /// <summary>
        /// Updates the bot's activity with the current posts.
        /// </summary>
        /// <returns></returns>
        public async Task UpdateActivityAsync()
        {
            // Extract a list from the stored posts
            List<Post> sortedPosts = Posts.Values.ToList();
            // Sort it
            sortedPosts.Sort();

            // Make an empty string to contain the new activity
            string newActivity = "";
            bool isFirst = true; // Make a variable to check whether or not this is the first time that the loop has run
            foreach (Post post in sortedPosts) // For each post, in the sorted order
            {
                // Add to the string
                newActivity += isFirst // If this is the first item in the loop
                    ? post.GetText() // Just put in that post's text
                    : $" | {post.GetText()}"; // If this is the second time onwards, add a divider between the messages

                // Set that this is not the first loop anymore, so that isFirst is only true the first time around
                isFirst = false;
            }

            // If the new string is longer than 128 characters (which is the character limit for a status) trim it down to 128 characters
            newActivity = newActivity.Length > 128 ? newActivity.Remove(128) : newActivity;

            // Set the new activity
            await Client.SetActivityAsync(new Game(newActivity)); // Chosen to make it a game, so all statuses will read "Playing [status]"
        }

        /// <summary>
        /// Checks if any posts have expired, and updates the bot's activity.
        /// </summary>
        /// <returns></returns>
        public async Task UpdatePostsAsync()
        {
            // Create a dictionary to hold the new list of posts
            ImmutableDictionary<string, Post> newPosts = Posts;

            // For each existing post
            foreach (Post post in Posts.Values)
            {
                // If the post's end date has passed
                if (post.EndDate <= DateTime.UtcNow.Date)
                {
                    // Remove that post from the new list
                    newPosts = newPosts.Remove(post.Name);
                }
            }

            // Set the newly created dictionary of posts, now expired-post-free, to the list of posts
            Posts = newPosts;

            // Update the bot's activity (both removing expired posts and updating dynamic tags)
            await UpdateActivityAsync();
            // Update the JSON (removing expired posts from it)
            await UpdateJsonAsync();
        }
    }
}
