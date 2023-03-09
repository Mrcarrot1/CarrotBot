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
using DSharpPlus.SlashCommands;
using KarrotObjectNotation;

namespace CarrotBot
{
    public static class Utils
    {
        private static readonly string version = "1.5.2";
        public static readonly string currentVersion = Program.isBeta ? $"{version}(beta)" : version;
        public static string yyMMdd = DateTime.Now.ToString("yyMMdd");
        public static DateTimeOffset startTime = DateTimeOffset.Now;
        public static string localDataPath = $@"{Directory.GetParent(Environment.CurrentDirectory)}/Data";
        //public static string localDataPath = @"/home/mrcarrot/Documents/CarrotBot/Data";
        public static string logsPath = $@"{Directory.GetParent(Environment.CurrentDirectory)}/Logs";
        public static string conversationDataPath = $@"{localDataPath}/Conversation";
        public static string levelingDataPath = $@"{localDataPath}/Leveling";

        public static int GuildCount = 0;

        //CarrotBot colors
        public static readonly DiscordColor CBGreen = new(15, 157, 88);
        public static readonly DiscordColor CBOrange = new(245, 124, 0);

        //Colors used in Discord UI elements
        public static readonly DiscordColor DiscordRed = new("#ED4245");
        public static readonly DiscordColor DiscordYellow = new("#F9A71A");
        public static readonly DiscordColor DiscordGreen = new("#3BA55D");
        public static readonly DiscordColor DiscordBlue = new("#5865F2");

        /// <summary>
        /// Takes a string that contains either a user ID or a user mention with ID and returns the ulong retrieved from that string, or throws a FormatException.
        /// </summary>
        /// <param name="mention"></param>
        /// <returns></returns>
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
            var resp = client.SendAsync(new HttpRequestMessage(HttpMethod.Head, URL)).GetAwaiter().GetResult();
            if (resp.Content.Headers.ContentType != null && resp.Content.Headers.ContentType.MediaType != null)
                return resp.Content.Headers.ContentType.MediaType.StartsWith("image/") || resp.Content.Headers.ContentType.MediaType.StartsWith("video/");
            else return false;
        }
#nullable disable
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
#nullable enable
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
            if (input == null) return "";
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
            if (input == null) return "";
            if (startIndex < 0 || length <= 0) return "";
            if (startIndex >= input.Length) return "";
            else if (startIndex + length > input.Length) return input.Substring(startIndex, input.Length - startIndex);
            else return input.Substring(startIndex, length);
        }
        public static bool TryLoadDatabaseNode(string inputPath, out KONNode output)
        {
#nullable disable
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
#nullable enable
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
        /// CarrotBot-specific extension method:
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
            eb.WithColor(color ?? CBGreen);
            await ctx.RespondAsync(embed: eb.Build());
        }

        /// <summary>
        /// CarrotBot-specific extension method: sends a text response to an interaction- not terribly useful actually
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task RespondAsync(this InteractionContext ctx, string message, bool ephemeral = false)
        {
            if (!ephemeral)
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(message));
            else
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(message).AsEphemeral());
        }

        /// <summary>
        /// CarrotBot-specific extension method: updates a previously indicated response with text content.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task UpdateResponseAsync(this InteractionContext ctx, string message)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(message));
        }

        /// <summary>
        /// CarrotBot-specific extension method: updates a previously indicated response with embed content.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="embed"></param>
        /// <returns></returns>
        public static async Task UpdateResponseAsync(this InteractionContext ctx, DiscordEmbed embed)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        /// <summary>
        /// CarrotBot-specific extension method: sends an embed response to an interaction- not terribly useful actually
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task RespondEmbedAsync(this InteractionContext ctx, string title, string content, DiscordColor? color = null)
        {
            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithTitle(title)
                .WithDescription(content)
                .WithColor(color ?? CBGreen)
                .Build();
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder(new DiscordMessageBuilder().WithEmbed(embed)));
        }

        /// <summary>
        /// CarrotBot-specific extension method: sends an embed response to an interaction- not terribly useful actually
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task RespondEmbedAsync(this InteractionContext ctx, DiscordEmbed embed)
        {
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder(new DiscordMessageBuilder().WithEmbed(embed)));
        }

        /// <summary>
        /// CarrotBot-specific extension method: sends a response to an interaction indicating that the bot is processing and will return with a result
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static async Task IndicateResponseAsync(this InteractionContext ctx, bool ephemeral = false)
        {
            if (!ephemeral)
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);
            else
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
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
                            foreach (string s in currentToken.Split(' '))
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

        public static int[] GetPossibleArgCounts(Command command)
        {
            List<int> output = new();

            if (!command.Overloads.Any())
            {
                output.Add(0);
            }
            else
            {
                foreach (CommandOverload overload in command.Overloads)
                {
                    foreach (int ovCount in getOverloadArgCounts(overload))
                    {
                        output.Add(ovCount);
                    }
                }
            }
            return output.ToArray();
        }

        private static int[] getOverloadArgCounts(CommandOverload overload)
        {
            List<int> output = new();

            if (!overload.Arguments.Any())
            {
                output.Add(0);
            }
            else
            {
                int required = 0, optional = 0;
                foreach (CommandArgument arg in overload.Arguments)
                {
                    if (arg.IsOptional) optional++;
                    else required++;
                }
                output.Add(required);
                for (int i = 1; i <= optional; i++)
                {
                    output.Add(required + i);
                }
            }
            return output.ToArray();
        }
    }
}