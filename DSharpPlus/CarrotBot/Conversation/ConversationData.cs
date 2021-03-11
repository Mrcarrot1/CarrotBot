using System;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace CarrotBot.Conversation
{
    public class ConversationData
    {
        public static ConversationMessage LastMessage { get; set; }
        public static List<ulong> MessageIdsInUse = new List<ulong>();
        public static List<ConversationChannel> ConversationChannels = new List<ConversationChannel>();
        public static Dictionary<ulong, ConversationMessage> ConversationMessages = new Dictionary<ulong, ConversationMessage>();
        public static Dictionary<ulong, ConversationMessage> ConversationMessagesByOrigId = new Dictionary<ulong, ConversationMessage>();
        public static List<ulong> AcceptedUsers = new List<ulong>();
        public static List<ulong> BannedUsers = new List<ulong>();
        public static List<ulong> Moderators = new List<ulong>();
        public static List<ulong> Administrators = new List<ulong>();

        public static ulong GenerateMessageId()
        {
            byte[] data = new byte[8];
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(data);
            ulong output = BitConverter.ToUInt64(data);
            while(MessageIdsInUse.Contains(output))
            {
                rng.GetBytes(data);
                output = BitConverter.ToUInt64(data);
            }
            return output;
        }
    }
}