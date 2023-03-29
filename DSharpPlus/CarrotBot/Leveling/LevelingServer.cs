using System;
using System.Collections.Generic;
using System.IO;
using KarrotObjectNotation;

namespace CarrotBot.Leveling
{
    public class LevelingServer
    {
        public ulong Id { get; }
        public Dictionary<ulong, LevelingUser> Users { get; internal set; }
        public List<LevelingUser> UsersByRank { get; internal set; }
        public Dictionary<int, ulong> RoleRewards { get; internal set; }
        public List<ulong> NoXPChannels { get; internal set; }
        public ulong? LevelUpChannel { get; internal set; }
        public bool MentionForLevelUp { get; internal set; }
        public Dictionary<int, string> LevelUpMessages { get; internal set; }
        public int XPCooldown { get; internal set; }
        public int XPPerLevel { get; internal set; }
        public int MinXPPerMessage { get; internal set; }
        public int MaxXPPerMessage { get; internal set; }
        public int XPRateOfChange { get; internal set; }
        public bool CumulativeRoles { get; internal set; }

        public LevelingUser CreateUser(ulong id, DateTimeOffset lastMessageTime, int xp = 5, int level = 0)
        {
            LevelingUser output = new LevelingUser(id, xp, level, this, lastMessageTime);

            Users.Add(id, output);
            UsersByRank.Add(output);
            FlushData();
            return output;
        }

        public LevelingServer(ulong id)
        {
            Id = id;
            Users = new Dictionary<ulong, LevelingUser>();
            UsersByRank = new List<LevelingUser>();
            RoleRewards = new Dictionary<int, ulong>();
            LevelUpMessages = new Dictionary<int, string>();
            NoXPChannels = new List<ulong>();
            MentionForLevelUp = false;
            XPCooldown = 60;
            XPPerLevel = 150;
            MinXPPerMessage = 5;
            MaxXPPerMessage = 5;
            XPRateOfChange = 1;
            if (!Directory.Exists($@"{Utils.levelingDataPath}/Server_{id}"))
                Directory.CreateDirectory($@"{Utils.levelingDataPath}/Server_{id}");
        }
        public void SortUsersByRank()
        {
            UsersByRank.Sort(LevelingData.CompareUsersByLevel);
        }
        public void FlushData()
        {
            if (Program.doNotWrite) return;
            KONNode node = new KONNode("LEVELING_SERVER");
            node.AddValue("id", Id);
            if (LevelUpChannel != null)
                node.AddValue("levelUpChannel", (ulong)LevelUpChannel);
            node.AddValue("xpCooldown", XPCooldown);
            node.AddValue("xpPerLevel", XPPerLevel);
            node.AddValue("minXPPerMessage", MinXPPerMessage);
            node.AddValue("maxXPPerMessage", MaxXPPerMessage);
            node.AddValue("cumulativeRoles", CumulativeRoles);
            node.AddValue("xpRateOfChange", XPRateOfChange);
            if (RoleRewards.Count > 0)
            {
                KONNode rolesNode = new KONNode("ROLES");
                foreach (KeyValuePair<int, ulong> role in RoleRewards)
                {
                    KONNode roleNode = new KONNode("ROLE");
                    roleNode.AddValue("id", role.Value);
                    roleNode.AddValue("level", role.Key);
                    rolesNode.AddChild(roleNode);
                }
                node.AddChild(rolesNode);
            }
            if (LevelUpMessages.Count > 0)
            {
                KONNode levelUpMsgNode = new KONNode("LEVEL_UP_MESSAGES");
                foreach (KeyValuePair<int, string> message in LevelUpMessages)
                {
                    KONNode messageNode = new KONNode("MESSAGE");
                    messageNode.AddValue("level", message.Key);
                    messageNode.AddValue("message", message.Value);
                    levelUpMsgNode.AddChild(messageNode);
                }
                node.AddChild(levelUpMsgNode);
            }
            if (NoXPChannels.Count > 0)
            {
                KONArray noXPChannelsArray = new KONArray("NO_XP_CHANNELS");
                foreach (ulong channel in NoXPChannels)
                {
                    noXPChannelsArray.AddItem(channel);
                }
                node.AddArray(noXPChannelsArray);
            }
            KONArray usersArray = new KONArray("USERS");
            foreach (LevelingUser user in UsersByRank)
            {
                usersArray.AddItem(user.Id);
            }
            node.AddArray(usersArray);
            File.WriteAllText($@"{Utils.levelingDataPath}/Server_{Id}/Index.cb", SensitiveInformation.EncryptDataFile(KONWriter.Default.Write(node)));
        }
        public void SetLevelUpChannel(ulong? channelId)
        {
            LevelUpChannel = channelId;
        }
        public int XPNeededForLevel(int level)
        {
            return ((level - 1) * XPRateOfChange + XPPerLevel); //In the form m(x - 1) + b, where x is the desired level, m is the slope of the line(rate at which XP increases per level change) and b is the y-intercept of that line(the value at level 1)
        }
        private static Random rnd = new Random();
        public int GetMessageXP()
        {
            return rnd.Next(MinXPPerMessage, MaxXPPerMessage + 1);
        }
    }
}