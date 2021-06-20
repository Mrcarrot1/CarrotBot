using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.IO;
using DSharpPlus.Entities;
using KarrotObjectNotation;

namespace CarrotBot.Conversation
{
    public class ConversationData
    {
        public static ConversationMessage LastMessage { get; set; }
        public static List<ulong> MessageIdsInUse = new List<ulong>();
        public static List<ConversationChannel> ConversationChannels = new List<ConversationChannel>();
        public static Dictionary<ulong, ConversationMessage> ConversationMessages = new Dictionary<ulong, ConversationMessage>();
        public static Dictionary<ulong, ConversationMessage> ConversationMessagesByOrigId = new Dictionary<ulong, ConversationMessage>();
        public static Dictionary<ulong, DiscordEmbed> ConversationEmbeds = new Dictionary<ulong, DiscordEmbed>();
        public static List<ulong> AcceptedUsers = new List<ulong>();
        public static List<ulong> BannedUsers = new List<ulong>();
        public static List<ulong> Moderators = new List<ulong>();
        public static List<ulong> Administrators = new List<ulong>();
        public static List<ulong> VerifiedUsers = new List<ulong>();
        public static Dictionary<ulong, PreVerifiedUser> PreVerifiedUsers = new Dictionary<ulong, PreVerifiedUser>();
        private static ulong currentMessageIndex = 0;

        public static ulong GenerateMessageId()
        {
            //Message ID meaning:
            //First 2 digits- year, e.g. 21 = 2021
            //Next 2 digits- month, e.g. 03 = March
            //Next 2 digits- date, e.g. 15 = 15th
            //The remaining digits are a sequential stream of numbers
            //e.g. 210315000000000045 is the 45th message on March 15, 2021
            //There will always be 12 digits following the date
            //This allows for a larger number of messages than can be practically sent while leaving zero chance of overflowing the ulong
            //And also allowing the conversation to continue working until at least 2100, by which time I will most likely be dead :)
            string Date = $"{DateTime.Now.ToString("yyMMdd")}000000000000";
            currentMessageIndex += 1;
            return ulong.Parse(Date) + currentMessageIndex;
        }
        public static void LoadDatabase()
        {
            KONNode databaseNode = KONParser.Default.Parse(File.ReadAllText($@"{Utils.conversationDataPath}/ConversationDatabase.cb"));
            Conversation.liveFeedChannel = Program.discord.GetChannelAsync(818960559625732096).Result;
            Conversation.embedsChannel = Program.discord.GetChannelAsync(824473207608049684).Result;
            ConversationChannels = new List<ConversationChannel>();
            PreVerifiedUsers = new Dictionary<ulong, PreVerifiedUser>();
            AcceptedUsers = new List<ulong>();
            Administrators = new List<ulong>();
            Moderators = new List<ulong>();
            BannedUsers = new List<ulong>();
            VerifiedUsers = new List<ulong>();
            foreach(KONNode childNode in databaseNode.Children)
            {
                if(childNode.Name == "CHANNELS")
                {
                    foreach(KONNode childNode2 in childNode.Children)
                    {
                        if(childNode2.Name == "CHANNEL")
                        {
                            bool ok = ulong.TryParse(childNode2.Values["id"], out ulong Id);
                            if (ok)
                                ConversationChannels.Add(new ConversationChannel(Id, childNode2.Values["name"]));
                        }
                    }
                }
                if(childNode.Name == "PREVERIFIED_USERS")
                {
                    foreach(KONNode childNode2 in childNode.Children)
                    {
                        if(childNode2.Name == "USER")
                        {
                            PreVerifiedUsers.Add(ulong.Parse(childNode2.Values["id"]), new PreVerifiedUser(ulong.Parse(childNode2.Values["id"]), int.Parse(childNode2.Values["messages"]), long.Parse(childNode2.Values["lastMessageTime"])));
                        }
                    }
                }
            }
            foreach(KONArray array in databaseNode.Arrays)
            {
                if(array.Name == "ACCEPTED_USERS")
                {
                    foreach(string str in array.Items)
                    {
                        if(!AcceptedUsers.Contains(ulong.Parse(str)))
                            AcceptedUsers.Add(ulong.Parse(str));
                    }
                }
                if(array.Name == "ADMINISTRATORS")
                {
                    foreach(string str in array.Items)
                    {
                        if(!Administrators.Contains(ulong.Parse(str)))
                            Administrators.Add(ulong.Parse(str));
                    }
                }
                if(array.Name == "MODERATORS")
                {
                    foreach(string str in array.Items)
                    {
                        if(!Moderators.Contains(ulong.Parse(str)))
                            Moderators.Add(ulong.Parse(str));
                    }
                }
                if(array.Name == "BANNED_USERS")
                {
                    foreach(string str in array.Items)
                    {
                        if(!BannedUsers.Contains(ulong.Parse(str)))
                            BannedUsers.Add(ulong.Parse(str));
                    }
                }
                if(array.Name == "VERIFIED_USERS")
                {
                    foreach(string str in array.Items)
                    {
                        if(!VerifiedUsers.Contains(ulong.Parse(str)))
                            VerifiedUsers.Add(ulong.Parse(str));
                    }
                }
            }
            /*foreach (string str in File.ReadAllLines($@"{Utils.conversationDataPath}/ConversationServers.csv"))
            {
                string[] values = str.Split(',');
                if (values[1] != null)
                {
                    bool ok = ulong.TryParse(values[0], out ulong Id);
                    if (ok)
                        ConversationData.ConversationChannels.Add(new ConversationChannel(Id, values[1]));
                }
            }
            foreach (string str in File.ReadAllText($@"{Utils.conversationDataPath}/AcceptedUsers.cb").Split(','))
            {
                if (ulong.TryParse(str, out ulong userId))
                    ConversationData.AcceptedUsers.Add(userId);
            }
            foreach (string str in File.ReadAllText($@"{Utils.conversationDataPath}/Administrators.cb").Split(','))
            {
                if (ulong.TryParse(str, out ulong userId))
                    ConversationData.Administrators.Add(userId);
            }
            foreach (string str in File.ReadAllText($@"{Utils.conversationDataPath}/Moderators.cb").Split(','))
            {
                if (ulong.TryParse(str, out ulong userId))
                    ConversationData.Moderators.Add(userId);
            }
            foreach (string str in File.ReadAllText($@"{Utils.conversationDataPath}/BannedUsers.cb").Split(','))
            {
                if (ulong.TryParse(str, out ulong userId))
                    ConversationData.BannedUsers.Add(userId);
            }
            foreach (string str in File.ReadAllText($@"{Utils.conversationDataPath}/VerifiedUsers.cb").Split(','))
            {
                if (ulong.TryParse(str, out ulong userId))
                    ConversationData.VerifiedUsers.Add(userId);
            }*/
        }
        public static void WriteDatabase()
        {
            KONNode databaseNode = new KONNode("CONVERSATION_DATABASE");

            KONNode channelsNode = new KONNode("CHANNELS");
            foreach(ConversationChannel channel in ConversationChannels)
            {
                KONNode channelNode = new KONNode("CHANNEL");
                channelNode.Values.Add("id", channel.Id.ToString());
                channelNode.Values.Add("name", channel.Server);
                channelsNode.AddChild(channelNode);
            }
            databaseNode.AddChild(channelsNode);

            KONNode preVerifiedUsersNode = new KONNode("PREVERIFIED_USERS");
            foreach(PreVerifiedUser user in PreVerifiedUsers.Values)
            {
                KONNode userNode = new KONNode("USER");
                userNode.AddValue("id", user.Id.ToString());
                userNode.AddValue("messages", user.MessagesSent.ToString());
                userNode.AddValue("lastMessageTime", user.LastMessageSentTime.ToUnixTimeSeconds().ToString());
                preVerifiedUsersNode.AddChild(userNode);
            }
            databaseNode.AddChild(preVerifiedUsersNode);

            KONArray acceptedUsers = new KONArray("ACCEPTED_USERS");
            foreach(ulong user in AcceptedUsers)
            {
                acceptedUsers.Items.Add(user.ToString());
            }
            KONArray administrators = new KONArray("ADMINISTRATORS");
            foreach(ulong user in Administrators)
            {
                administrators.Items.Add(user.ToString());
            }
            KONArray moderators = new KONArray("MODERATORS");
            foreach(ulong user in Moderators)
            {
                moderators.Items.Add(user.ToString());
            }
            KONArray bannedUsers = new KONArray("BANNED_USERS");
            foreach(ulong user in BannedUsers)
            {
                bannedUsers.Items.Add(user.ToString());
            }
            KONArray verifiedUsers = new KONArray("VERIFIED_USERS");
            foreach(ulong user in VerifiedUsers)
            {
                verifiedUsers.Items.Add(user.ToString());
            }
            databaseNode.AddArray(acceptedUsers);
            databaseNode.AddArray(administrators);
            databaseNode.AddArray(moderators);
            databaseNode.AddArray(bannedUsers);
            databaseNode.AddArray(verifiedUsers);
            File.WriteAllText($@"{Utils.conversationDataPath}/ConversationDatabase.cb", KONWriter.Default.Write(databaseNode));
        }
    }
}