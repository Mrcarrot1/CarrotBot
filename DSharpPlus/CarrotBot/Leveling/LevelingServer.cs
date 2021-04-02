using System;
using System.IO;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;

namespace CarrotBot.Leveling
{
    public class LevelingServer
    {
        public ulong Id { get; }
        public Dictionary<ulong, LevelingUser> Users { get; internal set; }
        public List<LevelingUser> UsersByRank { get; internal set; }
        public Dictionary<int, ulong> RoleRewards { get; internal set; }

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
            if(!Directory.Exists($@"{Utils.levelingDataPath}/Server_{id}"))
                Directory.CreateDirectory($@"{Utils.levelingDataPath}/Server_{id}");        
        }
        public void SortUsersByRank()
        {
            UsersByRank.Sort(LevelingData.CompareUsersByLevel);
        }
        public void FlushData()
        {
            ConfigNode node = new ConfigNode("LEVELING_SERVER");
            node.Values.Add("id", Id.ToString());
            ConfigNode rolesNode = new ConfigNode("ROLES");
            foreach(KeyValuePair<int, ulong> role in RoleRewards)
            {
                ConfigNode roleNode = new ConfigNode("ROLE");
                roleNode.AddValue("id", role.Value.ToString());
                roleNode.AddValue("level", role.Key.ToString());
                rolesNode.AddChild(roleNode);
            }
            node.AddChild(rolesNode);
            ConfigArray usersArray = new ConfigArray("USERS");
            foreach(LevelingUser user in UsersByRank)
            {
                usersArray.Items.Add(user.Id.ToString());
            }
            node.AddArray(usersArray);
            File.WriteAllText($@"{Utils.levelingDataPath}/Server_{Id}/Index.cb", ConfigWriter.Write(node));
        }
    }
}