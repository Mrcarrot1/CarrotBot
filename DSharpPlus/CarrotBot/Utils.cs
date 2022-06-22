using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Security.Cryptography;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using KarrotObjectNotation;

namespace CarrotBot
{
    public static class Utils
    {
        private static readonly string version = "1.3.2";
        public static readonly string currentVersion = Program.isBeta ? $"{version}(beta)" : version;
        public static string yyMMdd = DateTime.Now.ToString("yyMMdd");
        public static DateTimeOffset startTime = DateTimeOffset.Now;
        public static string localDataPath = $@"{Directory.GetParent(Environment.CurrentDirectory)}/Data";
        //public static string localDataPath = @"/home/mrcarrot/Documents/CarrotBot/Data";
        public static string logsPath = $@"{Directory.GetParent(Environment.CurrentDirectory)}/Logs";
        public static string conversationDataPath = $@"{localDataPath}/Conversation";
        public static string levelingDataPath = $@"{localDataPath}/Leveling";

        public static readonly DiscordColor CBGreen = new DiscordColor(15, 157, 88);

        public static int GuildCount = 0;

        public static readonly DiscordColor CBOrange = new DiscordColor(245, 124, 0);
        public static ulong GetId(string mention)
        {
            try
            {
                mention = mention
                    .Replace("<", "")
                    .Replace(">", "")
                    .Replace("!", "")
                    .Replace("@", "")
                    .Replace("#", "")
                    .Replace("&", "");
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
            catch (FormatException)
            {
                Id = 0;
                return false;
            }
            return true;
        }
        public static bool IsImageUrl(string URL)
        {
            HttpClient client = new HttpClient();
            var resp = client.SendAsync(new HttpRequestMessage(HttpMethod.Head, URL)).Result;
            return resp.Content.Headers.ContentType.MediaType.StartsWith("image/") || resp.Content.Headers.ContentType.MediaType.StartsWith("video/");
        }
        public static Task<DiscordMember> FindMemberAsync(this DiscordGuild guild, string user)
        {
            //Check to see if the input string is a user ID or mention
            if (TryGetId(user, out ulong Id))
            {
                if (guild.Members.ContainsKey(Id))
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
                foreach (DiscordMember member in guild.Members.Values)
                {
                    if (CompareStrings(member.Username, user) < currLowestDistance)
                    {
                        currLowestDistance = CompareStrings(member.Username, user);
                        currClosestMatch = member;
                    }
                    if (CompareStrings(member.Nickname, user) < currLowestDistance)
                    {
                        currLowestDistance = CompareStrings(member.Nickname, user);
                        currClosestMatch = member;
                    }
                    if (CompareStrings(member.Username.SafeSubstring(0, user.Length), user) < currLowestDistance)
                    {
                        currLowestDistance = CompareStrings(member.Username.SafeSubstring(0, user.Length), user);
                        currClosestMatch = member;
                    }
                    if (CompareStrings(member.Nickname.SafeSubstring(0, user.Length), user) < currLowestDistance)
                    {
                        currLowestDistance = CompareStrings(member.Nickname.SafeSubstring(0, user.Length), user);
                        currClosestMatch = member;
                    }
                }
                if (currClosestMatch != null)
                    return Task<DiscordMember>.Run(() => currClosestMatch);
            }
            catch (Exception e)
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
            if (s == null || t == null) return 1000000;
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
        /// Compares this string to another string and returns the Levenshtein distance between them.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="compareTo"></param>
        /// <returns></returns>
        public static int Compare(this string input, string compareTo)
        {
            return CompareStrings(input, compareTo);
        }
        /// <summary>
        /// Retrieves a substring from this instance, or empty if the start index is out of range.
        /// Designed as an exception-free wrapper around Substring.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static string SafeSubstring(this string input, int startIndex)
        {
            if (input == null) return null;
            if (startIndex < 0) return "";
            if (startIndex >= input.Length) return "";
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
            if (input == null) return null;
            if (startIndex < 0 || length <= 0) return "";
            if (startIndex >= input.Length) return "";
            else if (startIndex + length > input.Length) return input.Substring(startIndex, input.Length - startIndex);
            else return input.Substring(startIndex, length);
        }
        public static bool TryLoadDatabaseNode(string inputPath, out KONNode output)
        {
            if (!File.Exists(inputPath))
            {
                output = null;
                return false;
            }
            //If the file was last written to over 30 days ago, the data has expired and will be removed.
            //So we return nothing and delete the file.
            //Usually, the calling scope should also contain code to remove the reference to the file from wherever it was.
            //Also check for files marked as persistent- these shouldn't be removed.
            string DecryptedContents = SensitiveInformation.DecryptDataFile(File.ReadAllText(inputPath));
            if (DateTime.Now - File.GetLastWriteTime(inputPath) > new TimeSpan(30, 0, 0, 0) && !DecryptedContents.StartsWith("//PERSISTENT"))
            {
                output = null;
                File.Delete(inputPath);
                return false;
            }
            return KONParser.Default.TryParse(DecryptedContents, out output);
        }

        public static string GetUserFriendlyTypeName(Type type)
        {
            if (type == typeof(uint)) return "Whole Number";
            if (type == typeof(ulong)) return "ID Number"; //It's quite uncommon for CB to work with ulongs in any context other than IDs
            if (type == typeof(int) || type == typeof(long)) return "Integer";
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return "Decimal Number";
            if (type == typeof(bool)) return "True or False";
            if (type == typeof(string)) return "Text";

            //As a catch-all, just return the type's normal name otherwise
            return type.ToString();
        }

        /// <summary>
        /// A convenient way to send a response in embed form in one line of code.
        /// If more advanced features are needed, just stop being so lazy and use a DiscordEmbedBuilder.
        /// <para>&#160;</para>
        /// As a note, this comment was written to me(Mrcarrot), as I am the only person who actually works on this codebase(at least for now), and this method was written exclusively because I am lazy.
        /// Aren't extension methods great? *laughs at Java devs*
        /// <para>&#160;</para>
        /// I might be going insane.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static async Task RespondEmbedAsync(this CommandContext ctx, string title, string content, DiscordColor? color = null)
        {
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle(title);
            eb.WithDescription(content);
            eb.WithColor(color == null ? CBGreen : (DiscordColor)color);
            await ctx.RespondAsync(embed: eb.Build());
        }

        public static string[] TokenizeString(string str)
        {
            List<string> output = new List<string>();
            string currentToken = "";
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (char.IsWhiteSpace(c))
                {
                    if (currentToken != "")
                    {
                        output.Add(currentToken);
                        currentToken = "";
                    }
                }
                if (c == '"' || c == '\'')
                {
                    if (currentToken != "")
                    {
                        output.Add(currentToken);
                        currentToken = "";
                    }      
                    currentToken += c;
                    do 
                    {
                        i++;
                        if (i == str.Length)
                        {
                            foreach(string s in currentToken.Split(' '))
                            {
                                output.Add(s);
                            }
                            break;
                        }
                        currentToken += str[i];
                    } while (str[i] != c);
                    output.Add(currentToken);
                    currentToken = "";
                    continue;
                }
                currentToken += c;
            }
            if (currentToken != "")
                output.Add(currentToken);
            return output.ToArray();
        }
    }
}