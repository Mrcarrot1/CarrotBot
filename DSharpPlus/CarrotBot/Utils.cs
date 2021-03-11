using System;
using System.IO;

namespace CarrotBot
{
    public class Utils
    {
        
        public static string localDataPath = $@"{Directory.GetParent(Environment.CurrentDirectory)}/Data";
        public static string conversationDataPath = $@"{localDataPath}/Conversation";
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
            catch (System.Exception)
            {
                throw new FormatException();

            }
        }
    }
}