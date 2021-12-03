using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
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
        public static Dictionary<ulong, ConversationMessage> ConversationMessagesByOutId = new Dictionary<ulong, ConversationMessage>();
        public static Dictionary<ulong, DiscordEmbed> ConversationEmbeds = new Dictionary<ulong, DiscordEmbed>();
        public static List<ulong> AcceptedUsers = new List<ulong>();
        public static List<ulong> BannedUsers = new List<ulong>();
        public static List<ulong> Moderators = new List<ulong>();
        public static List<ulong> Administrators = new List<ulong>();
        public static List<ulong> SuperAdministrators = new List<ulong>();

        public static List<ulong> VerifiedUsers = new List<ulong>();
        public static Dictionary<ulong, PreVerifiedUser> PreVerifiedUsers = new Dictionary<ulong, PreVerifiedUser>();
        public static List<string> BannedWords = new List<string>();
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
            KONNode databaseNode = KONParser.Default.Parse(SensitiveInformation.DecryptDataFile(File.ReadAllText($@"{Utils.conversationDataPath}/ConversationDatabase.cb")));
            Conversation.liveFeedChannel = Program.discord.GetShard(388339196978266114).GetChannelAsync(818960559625732096).Result;
            Conversation.embedsChannel = Program.discord.GetShard(388339196978266114).GetChannelAsync(824473207608049684).Result;
            ConversationChannels = new List<ConversationChannel>();
            PreVerifiedUsers = new Dictionary<ulong, PreVerifiedUser>();
            AcceptedUsers = new List<ulong>();
            Administrators = new List<ulong>();
            SuperAdministrators = new List<ulong>();
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
                                ConversationChannels.Add(new ConversationChannel((ulong)childNode2.Values["id"], (string)childNode2.Values["name"], (ulong)childNode2.Values["guildId"]));
                        }
                    }
                }
                if(childNode.Name == "PREVERIFIED_USERS")
                {
                    foreach(KONNode childNode2 in childNode.Children)
                    {
                        if(childNode2.Name == "USER")
                        {
                            PreVerifiedUsers.Add((ulong)childNode2.Values["id"], new PreVerifiedUser((ulong)childNode2.Values["id"], (int)childNode2.Values["messages"], (long)childNode2.Values["lastMessageTime"]));
                        }
                    }
                }
            }
            foreach(KONArray array in databaseNode.Arrays)
            {
                if(array.Name == "ACCEPTED_USERS")
                {
                    foreach(ulong item in array.Items)
                    {
                        if(!AcceptedUsers.Contains(item))
                            AcceptedUsers.Add(item);
                    }
                }
                if(array.Name == "SUPER_ADMINISTRATORS")
                {
                    foreach(ulong item in array.Items)
                    {
                        if(!SuperAdministrators.Contains(item))
                            SuperAdministrators.Add(item);
                    }
                    if(!SuperAdministrators.Any())
                    {
                        SuperAdministrators.Add(Program.Mrcarrot.Id);
                    }
                }
                if(array.Name == "ADMINISTRATORS")
                {
                    foreach(ulong item in array.Items)
                    {
                        if(!Administrators.Contains(item))
                            Administrators.Add(item);
                    }
                }
                if(array.Name == "MODERATORS")
                {
                    foreach(ulong item in array.Items)
                    {
                        if(!Moderators.Contains(item))
                            Moderators.Add(item);
                    }
                }
                if(array.Name == "BANNED_USERS")
                {
                    foreach(ulong item in array.Items)
                    {
                        if(!BannedUsers.Contains(item))
                            BannedUsers.Add(item);
                    }
                }
                if(array.Name == "VERIFIED_USERS")
                {
                    foreach(ulong item in array.Items)
                    {
                        if(!VerifiedUsers.Contains(item))
                            VerifiedUsers.Add(item);
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
            if(Program.isBeta) return;
            KONNode databaseNode = new KONNode("CONVERSATION_DATABASE");

            KONNode channelsNode = new KONNode("CHANNELS");
            foreach(ConversationChannel channel in ConversationChannels)
            {
                KONNode channelNode = new KONNode("CHANNEL");
                channelNode.AddValue("id", channel.Id);
                channelNode.AddValue("name", channel.Server);
                channelNode.AddValue("guildId", channel.GuildId);
                channelsNode.AddChild(channelNode);
            }
            databaseNode.AddChild(channelsNode);

            KONNode preVerifiedUsersNode = new KONNode("PREVERIFIED_USERS");
            foreach(PreVerifiedUser user in PreVerifiedUsers.Values)
            {
                KONNode userNode = new KONNode("USER");
                userNode.AddValue("id", user.Id);
                userNode.AddValue("messages", user.MessagesSent);
                userNode.AddValue("lastMessageTime", user.LastMessageSentTime.ToUnixTimeSeconds());
                preVerifiedUsersNode.AddChild(userNode);
            }
            databaseNode.AddChild(preVerifiedUsersNode);

            KONArray acceptedUsers = new KONArray("ACCEPTED_USERS");
            foreach(ulong user in AcceptedUsers)
            {
                acceptedUsers.AddItem(user);
            }
            KONArray administrators = new KONArray("ADMINISTRATORS");
            foreach(ulong user in Administrators)
            {
                administrators.AddItem(user);
            }
            KONArray moderators = new KONArray("MODERATORS");
            foreach(ulong user in Moderators)
            {
                moderators.AddItem(user);
            }
            KONArray bannedUsers = new KONArray("BANNED_USERS");
            foreach(ulong user in BannedUsers)
            {
                bannedUsers.AddItem(user);
            }
            KONArray verifiedUsers = new KONArray("VERIFIED_USERS");
            foreach(ulong user in VerifiedUsers)
            {
                verifiedUsers.AddItem(user);
            }
            databaseNode.AddArray(acceptedUsers);
            databaseNode.AddArray(administrators);
            databaseNode.AddArray(moderators);
            databaseNode.AddArray(bannedUsers);
            databaseNode.AddArray(verifiedUsers);
            File.WriteAllText($@"{Utils.conversationDataPath}/ConversationDatabase.cb", SensitiveInformation.EncryptDataFile(KONWriter.Default.Write(databaseNode)));
        }
        public static void DeleteUserData(ulong userId)
        {
            if(AcceptedUsers.Contains(userId))
            {
                AcceptedUsers.Remove(userId);
            }
            if(VerifiedUsers.Contains(userId))
            {
                VerifiedUsers.Remove(userId);
            }
            if(PreVerifiedUsers.ContainsKey(userId))
            {
                PreVerifiedUsers.Remove(userId);
            }
        }
    }
}