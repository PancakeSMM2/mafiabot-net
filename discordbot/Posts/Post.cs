using Newtonsoft.Json;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace Mafiabot.Posts
{
    /// <summary>
    /// Represents a single post, with a name, end date, and text to display.
    /// </summary>
    internal class Post : IComparable<Post>, IComparable
    {
        /// <summary>
        /// The name of the post
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// The text that will be displayed in the bot's status
        /// </summary>
        public string DisplayText { get; private set; }
        /// <summary>
        /// The last day of the post
        /// </summary>
        public DateTime EndDate { get; private set; }

        /// <summary>
        /// Creates a new Post
        /// </summary>
        /// <param name="name">The name of the post</param>
        /// <param name="displayText">The text that will be displayed in the bot's status</param>
        /// <param name="endDate">The last day of the post</param>
        public Post(string name, string displayText, DateTime endDate)
        {
            // Store all provided variables
            Name = name ?? throw new ArgumentNullException(nameof(name)); // If the provided name was null, throw an exception
            DisplayText = displayText ?? ""; // If the provided text was null, replace it with an empty string
            EndDate = endDate; // Store the end date
        }

        /// <summary>
        /// Processes any dynamic tags like {N} and returns the resulting text.
        /// </summary>
        /// <returns>The processed text</returns>
        public string GetText()
        {
            // Create a variable to store the processed text
            string processedText = DisplayText;

            // If the provided text includes {N}
            if (DisplayText.Contains("{N}", StringComparison.CurrentCultureIgnoreCase))
            {
                // Calculate the days between the current day and the end day
                int daysLeft = (int)((EndDate.Ticks - DateTime.UtcNow.Date.Ticks) / TimeSpan.TicksPerDay);
                // Replace all instances of {N} with the calculated days
                processedText = processedText.Replace("{N}", daysLeft.ToString(), StringComparison.CurrentCultureIgnoreCase);
            }

            // If the provided text includes {S}
            if (DisplayText.Contains("{S}", StringComparison.CurrentCultureIgnoreCase))
            {
                // If there's one day until the end date, S is empty, otherwise put an 's' in the string
                string s = DateTime.UtcNow.Date.AddDays(1) == EndDate ? "" : "s";
                // Replace all instances of {S} with Schrodinger's 'S'
                processedText = processedText.Replace("{S}", s, StringComparison.CurrentCultureIgnoreCase);
            }

            /*
             * Space to add additional dynamic tags
             */

            // Return the fully processed text
            return processedText;
        }

        /// <summary>
        /// Compares this instance with a specified Post and indicates whether this instance precedes, follows, or appears in the same position in the sort order as the specified Post.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Post other)
        {
            // Compare the two dates
            // This ensures that the post that is ending the soonest will be displayed the most prominently
            return EndDate.CompareTo(other.EndDate);
        }

        /// <summary>
        /// Compares this instance with a specified Object and indicates whether this instance precedes, follows, or appears in the same position in the sort order as the specified Object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            // If the object is a Post
            if (obj is Post post)
            {
                // Compare the two
                return CompareTo(post);
            }

            // Otherwise, return 0 -- the two objects are incomparable
            return 0;
        }
    }
}
