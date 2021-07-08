//This class was created after the implementation of the conversation and leveling systems.
//As such, those systems do not use this database.
//This may change in the future, but for now I don't want to break already-functional code.
//That said, I will do my best to keep future data storage and handling in this class.
using System;
using System.Collections.Generic;
using System.IO;
using KarrotObjectNotation;

namespace CarrotBot.Data
{
    /// <summary>
    /// Contains general data not related to conversation or leveling.
    /// </summary>
    public class Database
    {
        public static Dictionary<ulong, GuildData> Guilds = new Dictionary<ulong, GuildData>();

        public static void Load()
        {
            Guilds = new Dictionary<ulong, GuildData>();
            KONNode DatabaseNode = KONParser.Default.Parse(File.ReadAllText($@"{Utils.localDataPath}/Database.cb"));
            foreach(KONArray array in DatabaseNode.Arrays)
            {
                if(array.Name == "GUILDS")
                {
                    foreach(string item in array.Items)
                    {
                        KONNode guildNode = KONParser.Default.Parse(File.ReadAllText($@"{Utils.localDataPath}/Guild_{item}/Index.cb"));
                        GuildData guild = new GuildData(ulong.Parse(item));
                        foreach(KONArray array1 in guildNode.Arrays)
                        {
                            if(array1.Name == "USERS")
                            {
                                foreach(string item1 in array1.Items)
                                {
                                    KONNode userNode = KONParser.Default.Parse(File.ReadAllText($@"{Utils.localDataPath}/Guild{item}/User_{item1}.cb"));
                                    GuildUserData user = new GuildUserData(ulong.Parse(item1), guild.Id);
                                    user.IsAFK = bool.Parse(userNode.Values["isAFK"]);
                                    if(user.IsAFK)
                                    {
                                        user.AFKMessage = userNode.Values["AFKMessage"];
                                        user.AFKTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(userNode.Values["AFKTime"]));
                                    }
                                    foreach(KONNode node in userNode.Children)
                                    {
                                        if(node.Name == "WARNING")
                                        {
                                            user.Warnings.Add(new Tuple<string, DateTimeOffset, ulong>(node.Values["reason"], 
                                            DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(node.Values["time"])), 
                                            ulong.Parse(node.Values["warnedBy"])));
                                        }
                                    }
                                    guild.Users.Add(user.Id, user);
                                }
                            }
                            if(array1.Name == "JOIN_ROLES")
                            {
                                foreach(string item1 in array1.Items)
                                {
                                    guild.AddJoinRole(ulong.Parse(item1));
                                }
                            }
                        }
                        Guilds.Add(guild.Id, guild);
                    }
                }
            }
        }
        public static GuildData GetOrCreateGuildData(ulong Id)
        {
            if(Guilds.ContainsKey(Id)) return Guilds[Id];
            GuildData output = new GuildData(Id, true);
            Guilds.Add(Id, output);
            return output;
        }
        public static void FlushAll()
        {
            if(Program.isBeta) return;
            foreach(KeyValuePair<ulong, GuildData> guild in Guilds)
            {
                guild.Value.FlushData(true);
            }
        }
    }
}