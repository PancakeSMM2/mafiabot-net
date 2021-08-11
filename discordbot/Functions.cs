using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace mafiabot
{
    static class Functions
    {
        public static async Task<bool> ToggleSnowflakeFromJSONAsync(ulong snowflake, string filePath)
        {
            return await Task.Run(() =>
            {
                string content = Convert.ToBase64String(File.ReadAllBytes(filePath));
                ulong[] output = JsonConvert.DeserializeObject<ulong[]>(content);

                bool remove = output.Contains(snowflake);
                if (remove) {
                    int index = Array.IndexOf(output, snowflake);
                    Array.Clear(output, index, 1);
                } else output.Append(snowflake);

                string newContent = JsonConvert.SerializeObject(output);
                File.WriteAllBytes(filePath, Convert.FromBase64String(newContent));
                return remove;
            });
        }
    }
}
