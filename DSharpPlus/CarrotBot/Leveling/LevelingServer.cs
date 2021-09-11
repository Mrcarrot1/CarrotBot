using System;
using System.IO;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
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

        public LevelingUser CreateUser(ulong id, DateTimeOffset lastMessageTime,  int xp = 5, int level = 0)
        {
            LevelingUser output =  new LevelingUser(id, xp, level, this, lastMessageTime);

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
            XPCooldown = 60;
            XPPerLevel = 150;
            MinXPPerMessage = 5;
            MaxXPPerMessage = 5;
            if(!Directory.Exists($@"{Utils.levelingDataPath}/Server_{id}"))
                Directory.CreateDirectory($@"{Utils.levelingDataPath}/Server_{id}");        
        }
        public void SortUsersByRank()
        {
            UsersByRank.Sort(LevelingData.CompareUsersByLevel);
        }
        public void FlushData()
        {
            if(Program.isBeta) return;
            KONNode node = new KONNode("LEVELING_SERVER");
            node.AddValue("id", Id);
            if(LevelUpChannel != null)
                node.AddValue("levelUpChannel", (ulong)LevelUpChannel);
            node.AddValue("xpCooldown", XPCooldown);
            node.AddValue("xpPerLevel", XPPerLevel);
            node.AddValue("minXPPerMessage", MinXPPerMessage);
            node.AddValue("maxXPPerMessage", MaxXPPerMessage);
            KONNode rolesNode = new KONNode("ROLES");
            foreach(KeyValuePair<int, ulong> role in RoleRewards)
            {
                KONNode roleNode = new KONNode("ROLE");
                roleNode.AddValue("id", role.Value);
                roleNode.AddValue("level", role.Key);
                rolesNode.AddChild(roleNode);
            }
            node.AddChild(rolesNode);
            KONNode levelUpMsgNode = new KONNode("LEVEL_UP_MESSAGES");
            foreach(KeyValuePair<int, string> message in LevelUpMessages)
            {
                KONNode messageNode = new KONNode("MESSAGE");
                messageNode.AddValue("level", message.Key);
                messageNode.AddValue("message", message.Value);
                levelUpMsgNode.AddChild(messageNode);
            }
            node.AddChild(levelUpMsgNode);
            KONArray usersArray = new KONArray("USERS");
            foreach(LevelingUser user in UsersByRank)
            {
                usersArray.AddItem(user.Id);
            }
            node.AddArray(usersArray);
            File.WriteAllText($@"{Utils.levelingDataPath}/Server_{Id}/Index.cb", KONWriter.Default.Write(node));
        }
        public void SetLevelUpChannel(ulong? channelId)
        {
            LevelUpChannel = channelId;
        }
        public int XPNeededForLevel(int level)
        {
            return (level * XPPerLevel);
        }
        private static Random rnd = new Random();
        public int GetMessageXP()
        {
            return rnd.Next(MinXPPerMessage, MaxXPPerMessage + 1);
        }
    }
}