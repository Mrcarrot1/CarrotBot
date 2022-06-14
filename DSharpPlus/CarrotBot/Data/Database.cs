//This class was created after the implementation of the conversation and leveling systems.
//As such, those systems do not use this database.
//This may change in the future, but for now I don't want to break already-functional code.
//That said, I will do my best to keep future data storage and handling in this class.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KarrotObjectNotation;

namespace CarrotBot.Data
{
    /// <summary>
    /// Contains general data not related to conversation or leveling.
    /// </summary>
    public class Database
    {
        public static Dictionary<ulong, GuildData> Guilds = new Dictionary<ulong, GuildData>();

        private static KONNode DatabaseNode = null;
        private static List<KONNode> RootNodes = new List<KONNode>();

        public static void Load()
        {
            Guilds = new Dictionary<ulong, GuildData>();
            RootNodes = new List<KONNode>();
            if (Program.doNotWrite) return;
            DatabaseNode = KONParser.Default.Parse(SensitiveInformation.DecryptDataFile(File.ReadAllText($@"{Utils.localDataPath}/Database.cb")));
            RootNodes.Add(DatabaseNode);
            foreach (KONArray array in DatabaseNode.Arrays)
            {
                if (array.Name == "GUILDS")
                {
                    foreach (ulong item in array.Items)
                    {
                        try
                        {
                            KONNode guildNode = KONParser.Default.Parse(SensitiveInformation.DecryptDataFile(File.ReadAllText($@"{Utils.localDataPath}/Guild_{item}/Index.cb")));
                            RootNodes.Add(guildNode);
                            GuildData guild = new GuildData(item);
                            guild.GuildPrefix = (string)guildNode.Values["prefix"];
                            guild.GuildPrefix = guild.GuildPrefix.Replace(@"\", "");
                            foreach (KONArray array1 in guildNode.Arrays)
                            {
                                if (array1.Name == "USERS")
                                {
                                    foreach (ulong item1 in array1.Items)
                                    {
                                        bool ok = Utils.TryLoadDatabaseNode($@"{Utils.localDataPath}/Guild_{item}/User_{item1}.cb", out KONNode userNode);
                                        if (!ok) continue;
                                        RootNodes.Add(userNode);
                                        GuildUserData user = new GuildUserData(item1, guild.Id);
                                        user.IsAFK = (bool)userNode.Values["isAFK"];
                                        if (user.IsAFK)
                                        {
                                            user.AFKMessage = (string)userNode.Values["AFKMessage"];
                                            user.AFKTime = DateTimeOffset.FromUnixTimeMilliseconds((long)userNode.Values["AFKTime"]);
                                        }
                                        foreach (KONNode node in userNode.Children)
                                        {
                                            if (node.Name == "WARNING")
                                            {
                                                if (node.Values.ContainsKey("message"))
                                                    user.Warnings.Add(new Tuple<string, DateTimeOffset, ulong>((string)node.Values["message"],
                                                    DateTimeOffset.FromUnixTimeMilliseconds((long)node.Values["time"]),
                                                    (ulong)node.Values["warnedBy"]));
                                                else
                                                    user.Warnings.Add(new Tuple<string, DateTimeOffset, ulong>("No reason given.",
                                                    DateTimeOffset.FromUnixTimeMilliseconds((long)node.Values["time"]),
                                                    (ulong)node.Values["warnedBy"]));
                                            }
                                        }
                                        guild.Users.Add(user.Id, user);
                                        if (userNode.Name == "USER")
                                            user.FlushData();
                                    }
                                }
                                if (array1.Name == "JOIN_ROLES")
                                {
                                    foreach (ulong item1 in array1.Items)
                                    {
                                        guild.AddJoinRole(item1);
                                    }
                                }
                                if (array1.Name == "JOIN_FILTERS")
                                {
                                    foreach (string item1 in array1.Items)
                                    {
                                        guild.JoinFilters.Add(new System.Text.RegularExpressions.Regex(item1));
                                    }
                                }
                                if (array1.Name == "JOIN_BLACKLIST")
                                {
                                    foreach (string item1 in array1.Items)
                                    {
                                        guild.JoinBlacklist.Add(item1);
                                    }
                                }
                            }
                            Guilds.Add(guild.Id, guild);
                            if (guildNode.Name == "GUILD")
                            {
                                guild.FlushData();
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Log($"Problem loading guild {item}: {e.ToString()}", Logger.CBLogLevel.EXC);
                        }
                    }
                }
            }
        }
        public static GuildData GetOrCreateGuildData(ulong Id)
        {
            if (Guilds.ContainsKey(Id)) return Guilds[Id];
            GuildData output = new GuildData(Id, true);
            Guilds.Add(Id, output);
            FlushDatabase();
            return output;
        }
        public static void FlushDatabase(bool flushAll = false)
        {
            if (Program.doNotWrite) return;
            DatabaseNode = new KONNode("DATABASE");
            KONArray guildsArray = new KONArray("GUILDS");
            foreach (GuildData guildData in Guilds.Values)
            {
                guildsArray.AddItem(guildData.Id);
            }
            DatabaseNode.AddArray(guildsArray);
            File.WriteAllText($@"{Utils.localDataPath}/Database.cb", SensitiveInformation.EncryptDataFile("//PERSISTENT\n" + KONWriter.Default.Write(DatabaseNode)));
            if (flushAll)
            {
                foreach (KeyValuePair<ulong, GuildData> guild in Guilds)
                {
                    guild.Value.FlushData(true);
                }
            }
        }
        /// <summary>
        /// Searches the database for a given key.
        /// </summary>
        /// <param name="input">Format ROOT_NODE/NODE/NODE/key</param>
        /// <returns>The corresponding value if the key exists, null if it does not.</returns>
        public static string Query(string input)
        {
            if (RootNodes.Count == 0) Load();
            string[] query = input.Split('/');
            KONNode currentNode = DatabaseNode;
            try
            {
                for (int i = 0; i < query.Length; i++)
                {
                    if (i != query.Length - 1)
                    {
                        currentNode = currentNode.Children.FirstOrDefault(x => x.Name == query[i]);
                    }
                    else
                    {
                        return currentNode.Values[query[i]].ToString();
                    }
                }
            }
            catch
            {
                return null;
            }
            return null;
        }
        public static void DeleteGuildData(ulong guildId)
        {
            if (Directory.Exists($@"{Utils.localDataPath}/Guild_{guildId}"))
            {
                Directory.Delete($@"{Utils.localDataPath}/Guild_{guildId}", true);
            }
            if (Guilds.ContainsKey(guildId))
            {
                Guilds.Remove(guildId);
            }
            FlushDatabase();
        }
        public static void DeleteUserData(ulong userId)
        {
            List<GuildData> guilds = Guilds.Values.ToList();
            for (int i = 0; i < Guilds.Count; i++)
            {
                if (guilds[i].Users.ContainsKey(userId))
                {
                    guilds[i].Users.Remove(userId);
                    File.Delete($@"{Utils.localDataPath}/Guild_{guilds[i].Id}/User_{userId}.cb");
                    guilds[i].FlushData();
                }
            }
        }
    }
}