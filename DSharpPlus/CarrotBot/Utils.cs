using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Globalization;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using KarrotObjectNotation;

namespace CarrotBot
{
    public static class Utils
    {
        private static readonly string version = "1.2.14";
        public static readonly string currentVersion = Program.isBeta ? $"{version}(beta)" : version;
        public static string localDataPath = $@"{Directory.GetParent(Environment.CurrentDirectory)}/Data";
        //public static string localDataPath = @"/home/mrcarrot/Documents/CarrotBot/Data";
        public static string logsPath = $@"{Directory.GetParent(Environment.CurrentDirectory)}/Logs";
        public static string conversationDataPath = $@"{localDataPath}/Conversation";
        public static string levelingDataPath = $@"{localDataPath}/Leveling";

        public static DiscordColor CBGreen = new DiscordColor(15, 157, 88);
        public static DiscordColor CBOrange = new DiscordColor(245, 124, 0);
        public static ulong GetId(string mention)
        {
            try
            {
                mention = mention
                    .Replace("<", "")
                    .Replace(">", "")
                    .Replace("!", "")
                    .Replace("@", "")
                    .Replace("#", "");
                ulong output = ulong.Parse(mention);
                return output;
            }
            catch
            {
                throw new FormatException();
            }
        }
        public static bool TryGetId(string mention, out ulong Id)
        {
            try
            {
                Id = GetId(mention);
            }
            catch(FormatException)
            {
                Id = 0;
                return false;
            }
            return true;
        }
        public static bool IsImageUrl(string URL)
        {
            var req = (HttpWebRequest)HttpWebRequest.Create(URL);
            req.Method = "HEAD";
            using (var resp = req.GetResponse())
            {
                return resp.ContentType.ToLower(CultureInfo.InvariantCulture)
                        .StartsWith("image/");
            }
        }
        public static Task<DiscordMember> FindMemberAsync(this DiscordGuild guild, string user)
        {
            //Check to see if the input string is a user ID or mention
            if(TryGetId(user, out ulong Id))
            {
                if(guild.Members.ContainsKey(Id))
                    return Task<DiscordMember>.Run(() => guild.Members[Id]);
            }
            //If not an ID- check for two things-
            //First, if there is less than 5 characters' difference between the input and either the username or nickname.
            //Second, if there is less than 5 characters' difference between the input and the substring from the beginning up to the length of the input.
            //SafeSubstring is used in case the input is longer than the user's name, 
            //and was created specifically for this purpose.
            //Feel free to borrow it.
            try
            {
                int currLowestDistance = user.Length < 3 ? user.Length + 1 : 3;
                DiscordMember currClosestMatch = null;
                foreach(DiscordMember member in guild.Members.Values)
                {
                    if(CompareStrings(member.Username, user) < currLowestDistance)
                    {
                        currLowestDistance = CompareStrings(member.Username, user);
                        currClosestMatch = member;
                    }
                    if(CompareStrings(member.Nickname, user) < currLowestDistance)
                    {
                        currLowestDistance = CompareStrings(member.Nickname, user);
                        currClosestMatch = member;
                    }
                    if(CompareStrings(member.Username.SafeSubstring(0, user.Length), user) < currLowestDistance)
                    {
                        currLowestDistance = CompareStrings(member.Username.SafeSubstring(0, user.Length), user);
                        currClosestMatch = member;
                    }
                    if(CompareStrings(member.Nickname.SafeSubstring(0, user.Length), user) < currLowestDistance)
                    {
                        currLowestDistance = CompareStrings(member.Nickname.SafeSubstring(0, user.Length), user);
                        currClosestMatch = member;
                    }
                }
                if(currClosestMatch != null)
                    return Task<DiscordMember>.Run(() => currClosestMatch);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            //If not found at the end, return null
            return null;
        }
        /// <summary>
        /// Gets the Levenshtein distance between two strings.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static int CompareStrings(string s, string t)
        {
            //If the string is null, we return an arbitrary large number-
            //This is not appropriate for all use cases, but here we don't want to create a false match between
            //null and a search query of fewer than 4 characters
            if(s == null || t == null) return 1000000;
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];
            
            // Verify arguments.
            if (n == 0)
            {
                return m;
            }
            
            if (m == 0)
            {
                return n;
            }
            
            // Initialize arrays.
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }
            
            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }
            
            // Begin looping.
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    // Compute cost.
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
                }
            }
            // Return cost.
            return d[n, m];
        }
        /// <summary>
        /// Retrieves a substring from this instance, or empty if the start index is out of range.
        /// Designed as an exception-free wrapper around Substring.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="startIndex"></param>43B581
        /// <returns></returns>
        public static string SafeSubstring(this string input, int startIndex)
        {
            if(input == null) return null;
            if(startIndex < 0) return "";
            if(startIndex >= input.Length) return "";
            else return input.Substring(startIndex);
        }
        /// <summary>
        /// Retrieves a substring from this instance, empty if the start index is out of range, or up to the end of the string.
        /// Designed as an exception-free wrapper around Substring.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string SafeSubstring(this string input, int startIndex, int length)
        {
            if(input == null) return null;
            if(startIndex < 0 || length <= 0) return "";
            if(startIndex >= input.Length) return "";
            else if(startIndex + length > input.Length) return input.Substring(startIndex, input.Length - startIndex);
            else return input.Substring(startIndex, length);
        }
        public static bool TryLoadDatabaseNode(string inputPath, out KONNode output)
        {
            if(!File.Exists(inputPath))
            {
                output = null;
                return false;
            }
            //If the file was last written to over 30 days ago, the data has expired and will be removed.
            //So we return nothing and delete the file.
            //Usually, the calling scope should also contain code to remove the reference to the file from wherever it was.
            if(DateTime.Now - File.GetLastWriteTime(inputPath) > new TimeSpan(30, 0, 0, 0))
            {
                output = null;
                File.Delete(inputPath);
                return false;
            }
            return KONParser.Default.TryParse(SensitiveInformation.DecryptDataFile(File.ReadAllText(inputPath)), out output);
        }
    }
}