using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSharpPlus.Entities;
using KarrotObjectNotation;

namespace CarrotBot.Conversation
{
    public static class ConversationData
    {
        public static ConversationMessage? LastMessage { get; set; }
        public static List<ulong> MessageIdsInUse = new List<ulong>();
        public static List<ConversationChannel> ConversationChannels = new List<ConversationChannel>();
        public static readonly Dictionary<ulong, ConversationMessage> ConversationMessages = new Dictionary<ulong, ConversationMessage>();
        public static readonly Dictionary<ulong, ConversationMessage> ConversationMessagesByOrigId = new Dictionary<ulong, ConversationMessage>();
        public static readonly Dictionary<ulong, ConversationMessage> ConversationMessagesByOutId = new Dictionary<ulong, ConversationMessage>();
        public static Dictionary<ulong, DiscordEmbed> ConversationEmbeds = new Dictionary<ulong, DiscordEmbed>();
        public static List<ulong> AcceptedUsers = new List<ulong>();
        public static List<ulong> BannedUsers = new List<ulong>();
        public static List<ulong> Moderators = new List<ulong>();
        public static List<ulong> Administrators = new List<ulong>();
        public static List<ulong> SuperAdministrators = new List<ulong>();

        public static List<ulong> VerifiedUsers = new List<ulong>();
        public static Dictionary<ulong, PreVerifiedUser> PreVerifiedUsers = new Dictionary<ulong, PreVerifiedUser>();
        public static List<string> BannedWords = new();
        private static ulong currentMessageIndex;

        public static bool MessageDataChangedSinceLastWrite { get; internal set; }


        /// <summary>
        /// The last time the message data for this run was written to disk
        /// </summary>
        public static DateTimeOffset lastMessagesWriteTime = Utils.startTime;

        public static ulong GenerateMessageId()
        {
            //Message ID meaning:
            //First 4 digits- year, e.g. 2021
            //Next 2 digits- month, e.g. 03 = March
            //Next 2 digits- date, e.g. 15 = 15th
            //The remaining digits are a sequential stream of numbers
            //e.g. 20210315000000000045 is the 45th message on 15 March 2021
            //There will always be 9 digits following the date
            //This allows for a larger number of messages than can be practically sent while leaving zero chance of overflowing the ulong
            //And also allowing the conversation to continue working until at least 2100, by which time I will most likely be dead :)
            string Date = $"{DateTime.Now:yyyyMMdd}000000000";
            currentMessageIndex += 1;
            return ulong.Parse(Date) + currentMessageIndex;
        }
        public static void LoadDatabase()
        {
#nullable disable
            KONNode databaseNode = KONParser.Default.Parse(SensitiveInformation.AES256ReadFile($@"{Utils.conversationDataPath}/ConversationDatabase.cb"));
            Conversation.liveFeedChannel = Program.discord!.GetShard(388339196978266114).GetChannelAsync(818960559625732096).GetAwaiter().GetResult();
            Conversation.embedsChannel = Program.discord.GetShard(388339196978266114).GetChannelAsync(824473207608049684).GetAwaiter().GetResult();
            ConversationChannels = new List<ConversationChannel>();
            PreVerifiedUsers = new Dictionary<ulong, PreVerifiedUser>();
            AcceptedUsers = new List<ulong>();
            Administrators = new List<ulong>();
            SuperAdministrators = new List<ulong>();
            Moderators = new List<ulong>();
            BannedUsers = new List<ulong>();
            VerifiedUsers = new List<ulong>();
            foreach (KONNode childNode in databaseNode.Children)
            {
                if (childNode.Name == "CHANNELS")
                {
                    foreach (KONNode childNode2 in childNode.Children)
                    {
                        if (childNode2.Name == "CHANNEL")
                        {
                            ConversationChannels.Add(new ConversationChannel((ulong)childNode2.Values["id"], (string)childNode2.Values["name"], (ulong)childNode2.Values["guildId"]));
                        }
                    }
                }
                if (childNode.Name == "PREVERIFIED_USERS")
                {
                    foreach (KONNode childNode2 in childNode.Children)
                    {
                        if (childNode2.Name == "USER")
                        {
                            PreVerifiedUsers.Add((ulong)childNode2.Values["id"], new PreVerifiedUser((ulong)childNode2.Values["id"], (int)childNode2.Values["messages"], (long)childNode2.Values["lastMessageTime"]));
                        }
                    }
                }
            }
            foreach (KONArray array in databaseNode.Arrays)
            {
                if (array.Name == "ACCEPTED_USERS")
                {
                    foreach (ulong item in array.Items)
                    {
                        if (!AcceptedUsers.Contains(item))
                            AcceptedUsers.Add(item);
                    }
                }
                if (array.Name == "SUPER_ADMINISTRATORS")
                {
                    foreach (ulong item in array.Items)
                    {
                        if (!SuperAdministrators.Contains(item))
                            SuperAdministrators.Add(item);
                    }
                    if (!SuperAdministrators.Any())
                    {
                        SuperAdministrators.Add(Program.Mrcarrot.Id);
                    }
                }
                if (array.Name == "ADMINISTRATORS")
                {
                    foreach (ulong item in array.Items)
                    {
                        if (!Administrators.Contains(item))
                            Administrators.Add(item);
                    }
                }
                if (array.Name == "MODERATORS")
                {
                    foreach (ulong item in array.Items)
                    {
                        if (!Moderators.Contains(item))
                            Moderators.Add(item);
                    }
                }
                if (array.Name == "BANNED_USERS")
                {
                    foreach (ulong item in array.Items)
                    {
                        if (!BannedUsers.Contains(item))
                            BannedUsers.Add(item);
                    }
                }
                if (array.Name == "VERIFIED_USERS")
                {
                    foreach (ulong item in array.Items)
                    {
                        if (!VerifiedUsers.Contains(item))
                            VerifiedUsers.Add(item);
                    }
                }
                if (Directory.Exists($@"{Utils.conversationDataPath}/Messages"))
                {
                    foreach (string file in Directory.GetFiles($@"{Utils.conversationDataPath}/Messages"))
                    {
                        if (Utils.TryLoadDatabaseNode(file, out KONNode msgsNode))
                        {
                            Logger.Log($"Loading conversation messages for {msgsNode.Values["date"]}");
                            foreach (KONNode node in msgsNode.Children)
                            {
                                try
                                {
                                    if (node.Name == "MESSAGE")
                                    {
                                        try
                                        {
                                            ulong CBId = (ulong)node.Values["CBId"];
                                            ulong originalChannelId = (ulong)node.Values["originalChannel"];
                                            ulong originalId = (ulong)node.Values["originalId"];
                                            DiscordMessage originalMsg = null;
                                            ConversationChannel originalChannel = null;
                                            DiscordMember author = null;
                                            if (ConversationChannels.Any(x => x.Id == originalChannelId))
                                            {
                                                originalChannel = ConversationChannels.FirstOrDefault(x => x.Id == originalChannelId);
                                                DiscordChannel origChn = Program.discord.GetShard(originalChannel.GuildId).GetChannelAsync(originalChannelId).GetAwaiter().GetResult();
                                                originalMsg = origChn.GetMessageAsync(originalId).GetAwaiter().GetResult();
                                                author = origChn.Guild.GetMemberAsync(originalMsg.Author.Id).GetAwaiter().GetResult();
                                            }
                                            ConversationMessage message = new(CBId, originalMsg, author, originalChannel)
                                            {
                                                thisRun = false
                                            };
                                            foreach (KONNode childNode in node.Children)
                                            {
                                                if (childNode.Name == "CHANNEL_MESSAGES")
                                                {
                                                    foreach (KeyValuePair<string, object> chnMsg in childNode.Values)
                                                    {
                                                        try
                                                        {
                                                            if (ConversationChannels.Any(x => x.Id.ToString() == chnMsg.Key))
                                                            {
                                                                ulong msgId = (ulong)chnMsg.Value;
                                                                ConversationChannel channel1 = ConversationChannels.FirstOrDefault(x => x.Id.ToString() == chnMsg.Key);
                                                                DiscordChannel channel = Program.discord.GetShard(channel1.GuildId).GetChannelAsync(channel1.Id).GetAwaiter().GetResult();
                                                                DiscordMessage msg = channel.GetMessageAsync(msgId).GetAwaiter().GetResult();
                                                                message.ChannelMessages.Add(channel1.Id, msg);
                                                                ConversationMessagesByOutId.Add(msgId, message);
                                                            }
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            Logger.Log(e.ToString(), Logger.CBLogLevel.EXC);
                                                        }
                                                    }
                                                }
                                            }
                                            ConversationMessages.Add(CBId, message);
                                            ConversationMessagesByOrigId.Add(originalId, message);
                                        }
                                        catch (Exception e)
                                        {
                                            Logger.Log(e.ToString(), Logger.CBLogLevel.EXC);
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Logger.Log(e.ToString(), Logger.CBLogLevel.EXC);
                                }
                            }
                        }
                    }
                }
#nullable enable
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
            if (Program.doNotWrite) return;
            KONNode databaseNode = new KONNode("CONVERSATION_DATABASE");

            KONNode channelsNode = new KONNode("CHANNELS");
            foreach (ConversationChannel? channel in ConversationChannels)
            {
                KONNode channelNode = new KONNode("CHANNEL");
                channelNode.AddValue("id", channel.Id);
                channelNode.AddValue("name", channel.CallSign);
                channelNode.AddValue("guildId", channel.GuildId);
                channelsNode.AddChild(channelNode);
            }
            databaseNode.AddChild(channelsNode);

            KONNode preVerifiedUsersNode = new KONNode("PREVERIFIED_USERS");
            foreach (PreVerifiedUser user in PreVerifiedUsers.Values)
            {
                KONNode userNode = new KONNode("USER");
                userNode.AddValue("id", user.Id);
                userNode.AddValue("messages", user.MessagesSent);
                userNode.AddValue("lastMessageTime", user.LastMessageSentTime.ToUnixTimeSeconds());
                preVerifiedUsersNode.AddChild(userNode);
            }
            databaseNode.AddChild(preVerifiedUsersNode);

            KONArray acceptedUsers = new KONArray("ACCEPTED_USERS");
            foreach (ulong user in AcceptedUsers)
            {
                acceptedUsers.AddItem(user);
            }
            KONArray administrators = new KONArray("ADMINISTRATORS");
            foreach (ulong user in Administrators)
            {
                administrators.AddItem(user);
            }
            KONArray moderators = new KONArray("MODERATORS");
            foreach (ulong user in Moderators)
            {
                moderators.AddItem(user);
            }
            KONArray bannedUsers = new KONArray("BANNED_USERS");
            foreach (ulong user in BannedUsers)
            {
                bannedUsers.AddItem(user);
            }
            KONArray verifiedUsers = new KONArray("VERIFIED_USERS");
            foreach (ulong user in VerifiedUsers)
            {
                verifiedUsers.AddItem(user);
            }
            databaseNode.AddArray(acceptedUsers);
            databaseNode.AddArray(administrators);
            databaseNode.AddArray(moderators);
            databaseNode.AddArray(bannedUsers);
            databaseNode.AddArray(verifiedUsers);
            SensitiveInformation.AES256WriteFile($@"{Utils.conversationDataPath}/ConversationDatabase.cb", KONWriter.Default.Write(databaseNode));
        }


        /// <summary>
        /// Writes the current message data to disk
        /// </summary>
        public static void WriteMessageData(bool midnightOverride = false)
        {
            if (!MessageDataChangedSinceLastWrite) return;
            //If it's less than 5 minutes from midnight and the override wasn't set, don't write data.
            //The override is only ever set by the Conversation message handler, which takes over from the automatic writing at 23:55
            if ((DateTimeOffset.Now + new TimeSpan(0, 5, 0)).Day > DateTimeOffset.Now.Day && !midnightOverride) return;
            if (!Directory.Exists($@"{Utils.conversationDataPath}/Messages"))
                Directory.CreateDirectory($@"{Utils.conversationDataPath}/Messages");
            KONNode node = new KONNode("CONVERSATION_MESSAGES");
            node.AddValue("date", Utils.yyMMdd);
            foreach (ConversationMessage message in ConversationMessages.Values)
            {
                if (!message.thisRun) continue;
                KONNode msgNode = new KONNode("MESSAGE");
                msgNode.AddValue("CBId", message.Id);
                if (message.originalChannel is not null) msgNode.AddValue("originalChannel", message.originalChannel.Id);
                msgNode.AddValue("originalId", message.originalMessage.Id);
                KONNode channelMessages = new KONNode("CHANNEL_MESSAGES");
                foreach (KeyValuePair<ulong, DiscordMessage> chnMsg in message.ChannelMessages)
                {
                    channelMessages.AddValue(chnMsg.Key.ToString(), chnMsg.Value);
                }
                msgNode.AddChild(channelMessages);
                node.AddChild(msgNode);
            }
            SensitiveInformation.AES256WriteFile($@"{Utils.conversationDataPath}/Messages/{Utils.startTime.ToUnixTimeSeconds()}.cb", KONWriter.Default.Write(node));
            lastMessagesWriteTime = DateTimeOffset.Now;
            MessageDataChangedSinceLastWrite = false;
        }
        public static void DeleteUserData(ulong userId)
        {
            if (AcceptedUsers.Contains(userId))
            {
                AcceptedUsers.Remove(userId);
            }
            if (VerifiedUsers.Contains(userId))
            {
                VerifiedUsers.Remove(userId);
            }
            if (PreVerifiedUsers.ContainsKey(userId))
            {
                PreVerifiedUsers.Remove(userId);
            }
        }
    }
}