using System;
using System.IO;
using System.Collections.Generic;
using DSharpPlus;

namespace CarrotBot.Leveling
{
    public class LevelingData
    {
        public static Dictionary<ulong, LevelingServer> Servers = new Dictionary<ulong, LevelingServer>();
        public static Dictionary<int, ulong> RoleRewards = new Dictionary<int, ulong>();

        public static void LoadDatabase()
        {
            Servers = new Dictionary<ulong, LevelingServer>();
            RoleRewards = new Dictionary<int, ulong>();
            ConfigNode node = ConfigParser.Parse(File.ReadAllText($@"{Utils.levelingDataPath}/LevelingDatabase.cb"));
            /*foreach(ConfigNode childNode in node.Children)
            {
                if(childNode.Name == "SERVER")
                {
                    LevelingServer server = new LevelingServer(ulong.Parse(childNode.Values["id"]));
                }
            }*/
            foreach(ConfigArray array in node.Arrays)
            {
                if(array.Name == "SERVERS")
                {
                    foreach(string item in array.Items)
                    {
                        ConfigNode serverIndex = ConfigParser.Parse(File.ReadAllText($@"{Utils.levelingDataPath}/Server_{item}/Index.cb"));
                        LevelingServer server = new LevelingServer(ulong.Parse(item));
                        foreach(ConfigNode childNode in serverIndex.Children)
                        {
                            if(childNode.Name == "ROLES")
                            {
                                foreach(ConfigNode childNode2 in childNode.Children)
                                {
                                    server.RoleRewards.Add(int.Parse(childNode2.Values["level"]), ulong.Parse(childNode2.Values["id"]));
                                }
                            }
                        }
                        foreach(ConfigArray array1 in serverIndex.Arrays)
                        {
                            if(array1.Name == "USERS")
                            {
                                foreach(string Item in array1.Items)
                                {
                                    ConfigNode userNode = ConfigParser.Parse(File.ReadAllText($@"{Utils.levelingDataPath}/Server_{item}/User_{Item}.cb"));
                                    LevelingUser user = new LevelingUser(ulong.Parse(Item), int.Parse(userNode.Values["xp"]), int.Parse(userNode.Values["level"]), server, DateTimeOffset.FromUnixTimeSeconds(long.Parse(userNode.Values["lastMessageTime"])));
                                    server.Users.Add(ulong.Parse(Item), user);
                                    server.UsersByRank.Add(user);
                                }
                            }
                        }
                        server.SortUsersByRank();
                        Servers.Add(server.Id, server);
                    }
                }
            }
        }
        public static int XPNeededForLevel(int level)
        {
            return (level * 150);
        }
        /// <summary>
        /// Determines whether the first user is a higher level than the second.
        /// </summary>
        /// <param name="user1">The first user is </param>
        /// <param name="user2"></param>
        /// <returns></returns>
        public static bool IsHigherLevel(LevelingUser user1, LevelingUser user2)
        {
            if(user1.Level > user2.Level)
            {
                return true;
            }
            if(user1.Level == user2.Level)
            {
                return user1.CurrentXP > user2.CurrentXP;
            }
            if(user1.Level < user2.Level)
            {
                return false;
            }
            return false;
        }
        public static int CompareUsersByLevel(LevelingUser x, LevelingUser y)
        {
            if(IsHigherLevel(x, y))
                return -1;
            if(IsHigherLevel(y, x))
                return 1;
            return 0; //If x > y and y > x are both false, they're equal
        }
        /*public static List<LevelingUser> SortUsersByRank(List<LevelingUser> input)
        {
            List<LevelingUser> output = new List<LevelingUser>();
            for(int i = 0; i < input.Count; i++)
            {
                if(i == 0) output.Add(input[0]); //Nothing to compare to in the list if this is the first item we're adding
                else
                {
                    for(int j = 0; j < output.Count; j++)
                    {
                        if(IsHigherLevel(input[i], output[j]))
                        {
                            output.Insert(j, input[i]);
                            break;
                        }
                        else
                        {
                            if(j == output.Count - 1)
                            {
                                output.Add(input[i]);
                                break;
                            }
                            else continue;
                        }
                    }
                }
            }
            return output;
            return input.Sort(CompareUsersByLevel)
        }*/
    }
}