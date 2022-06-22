using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DSharpPlus;
using KarrotObjectNotation;

namespace CarrotBot.Leveling
{
    public class LevelingData
    {
        public static Dictionary<ulong, LevelingServer> Servers = new Dictionary<ulong, LevelingServer>();

        public static void LoadDatabase()
        {
            Servers = new Dictionary<ulong, LevelingServer>();
            KONNode node = KONParser.Default.Parse(File.ReadAllText($@"{Utils.levelingDataPath}/LevelingDatabase.cb"));
            /*foreach(KONNode childNode in node.Children)
            {
                if(childNode.Name == "SERVER")
                {
                    LevelingServer server = new LevelingServer(ulong.Parse(childNode.Values["id"]));
                }
            }*/
            foreach (KONArray array in node.Arrays)
            {
                if (array.Name == "SERVERS")
                {
                    foreach (ulong item in array.Items)
                    {
                        KONNode serverIndex = KONParser.Default.Parse(SensitiveInformation.DecryptDataFile(File.ReadAllText($@"{Utils.levelingDataPath}/Server_{item}/Index.cb")));
                        LevelingServer server = new LevelingServer(item);
                        if (serverIndex.Values.ContainsKey("levelUpChannel"))
                            server.SetLevelUpChannel((ulong)serverIndex.Values["levelUpChannel"]);
                        if (serverIndex.Values.ContainsKey("xpCooldown"))
                            server.XPCooldown = (int)serverIndex.Values["xpCooldown"];
                        if (serverIndex.Values.ContainsKey("xpPerLevel"))
                            server.XPPerLevel = (int)serverIndex.Values["xpPerLevel"];
                        if (serverIndex.Values.ContainsKey("minXPPerMessage"))
                            server.MinXPPerMessage = (int)serverIndex.Values["minXPPerMessage"];
                        if (serverIndex.Values.ContainsKey("maxXPPerMessage"))
                            server.MaxXPPerMessage = (int)serverIndex.Values["maxXPPerMessage"];
                        if (serverIndex.Values.ContainsKey("cumulativeRoles"))
                            server.CumulativeRoles = (bool)serverIndex.Values["cumulativeRoles"];
                        foreach (KONNode childNode in serverIndex.Children)
                        {
                            if (childNode.Name == "ROLES")
                            {
                                foreach (KONNode childNode2 in childNode.Children)
                                {
                                    server.RoleRewards.Add((int)childNode2.Values["level"], (ulong)childNode2.Values["id"]);
                                }
                            }
                        }
                        foreach (KONArray array1 in serverIndex.Arrays)
                        {
                            if (array1.Name == "USERS")
                            {
                                foreach (ulong Item in array1.Items)
                                {
                                    bool ok = Utils.TryLoadDatabaseNode($@"{Utils.levelingDataPath}/Server_{item}/User_{Item}.cb", out KONNode userNode);
                                    if (!ok) continue;
                                    LevelingUser user = new LevelingUser(Item, (int)userNode.Values["xp"], (int)userNode.Values["level"], server, DateTimeOffset.FromUnixTimeSeconds((long)userNode.Values["lastMessageTime"]));
                                    server.Users.Add(Item, user);
                                    server.UsersByRank.Add(user);
                                }
                            }
                            if (array1.Name == "NO_XP_CHANNELS")
                            {
                                foreach (ulong channel in array1.Items)
                                {
                                    server.NoXPChannels.Add(channel);
                                }
                            }
                        }
                        server.SortUsersByRank();
                        Servers.Add(server.Id, server);
                    }
                }
            }
        }
        /// <summary>
        /// Determines whether the first user is a higher level than the second.
        /// </summary>
        /// <param name="user1">The first user is </param>
        /// <param name="user2"></param>
        /// <returns></returns>
        public static bool IsHigherLevel(LevelingUser user1, LevelingUser user2)
        {
            if (user1.Level > user2.Level)
            {
                return true;
            }
            if (user1.Level == user2.Level)
            {
                return user1.CurrentXP > user2.CurrentXP;
            }
            if (user1.Level < user2.Level)
            {
                return false;
            }
            return false;
        }
        public static int CompareUsersByLevel(LevelingUser x, LevelingUser y)
        {
            if (IsHigherLevel(x, y))
                return -1;
            if (IsHigherLevel(y, x))
                return 1;
            return 0; //If x > y and y > x are both false, they're equal
        }
        public static void FlushServerList()
        {
            if (Program.doNotWrite) return;
            KONNode node = new KONNode("LEVELING_DATABASE");
            KONArray serversArray = new KONArray("SERVERS");
            foreach (KeyValuePair<ulong, LevelingServer> server in Servers)
            {
                serversArray.Items.Add(server.Key);
            }
            node.AddArray(serversArray);
            File.WriteAllText($@"{Utils.levelingDataPath}/LevelingDatabase.cb", "//PERSISTENT\n" + KONWriter.Default.Write(node));
        }
        public static void FlushAllData()
        {
            if (Program.doNotWrite) return;
            FlushServerList();
            foreach (KeyValuePair<ulong, LevelingServer> server in Servers)
            {
                server.Value.FlushData();
                foreach (LevelingUser user in server.Value.UsersByRank)
                {
                    user.FlushData();
                }
            }
        }
        public static void AddServer(ulong Id)
        {
            if (Servers.ContainsKey(Id)) return;
            LevelingServer server = new LevelingServer(Id);
            Servers.Add(Id, server);
            FlushServerList();
        }
        public static void RemoveServer(ulong Id, bool deleteData)
        {
            if (Servers.ContainsKey(Id))
                Servers.Remove(Id);
            else return;
            FlushServerList();
            if (deleteData)
            {
                Directory.Delete($@"{Utils.levelingDataPath}/Server_{Id}", true);
            }
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
        public static void DeleteGuildData(ulong guildId)
        {
            if (Directory.Exists($@"{Utils.levelingDataPath}/Guild_{guildId}"))
            {
                Directory.Delete($@"{Utils.levelingDataPath}/Guild_{guildId}", true);
            }
            if (Servers.ContainsKey(guildId))
            {
                Servers.Remove(guildId);
            }
            FlushServerList();
        }
        public static void DeleteUserData(ulong userId)
        {
            List<LevelingServer> guilds = Servers.Values.ToList();
            for (int i = 0; i < guilds.Count; i++)
            {
                if (guilds[i].Users.ContainsKey(userId))
                {
                    guilds[i].Users.Remove(userId);
                    File.Delete($@"{Utils.levelingDataPath}/Server_{guilds[i].Id}/User_{userId}.cb");
                    guilds[i].FlushData();
                }
            }
        }
    }
}