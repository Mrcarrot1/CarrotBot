using System;
using System.IO;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;

namespace CarrotBot
{
    public class Utils
    {
        private static readonly string version = "1.1.0";
        public static readonly string currentVersion = Program.isBeta ? $"{version}(beta)" : version;
        public static string localDataPath = $@"{Directory.GetParent(Environment.CurrentDirectory)}/Data";
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
    }
}