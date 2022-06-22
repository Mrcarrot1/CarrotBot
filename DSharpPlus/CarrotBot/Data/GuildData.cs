using System;
using System.Collections.Generic;
using System.IO;
using KarrotObjectNotation;
using System.Text.RegularExpressions;

namespace CarrotBot.Data
{
    public class GuildData
    {
        public ulong Id { get; set; }
        public Dictionary<ulong, GuildUserData> Users { get; internal set; }

        public List<ulong> RolesToAssignOnJoin { get; internal set; }

        public string GuildPrefix { get; internal set; }

        public List<JoinFilter> JoinFilters { get; internal set; }

        public List<JoinBlacklist> JoinBlacklists { get; internal set; }

        public void FlushData(bool flushUserData = false)
        {
            if (Program.doNotWrite) return;
            KONNode node = new KONNode($"GUILD_{Id}");
            node.AddValue("id", Id);
            node.AddValue("prefix", GuildPrefix);

            KONArray usersArray = new KONArray("USERS");
            foreach (KeyValuePair<ulong, GuildUserData> user in Users)
            {
                usersArray.Items.Add(user.Key);
                if (flushUserData) user.Value.FlushData();
            }
            node.AddArray(usersArray);

            KONArray joinRolesArray = new KONArray("JOIN_ROLES");
            foreach (ulong role in RolesToAssignOnJoin)
            {
                joinRolesArray.Items.Add(role);
            }
            node.AddArray(joinRolesArray);

            KONNode regexFiltersNode = new KONNode("JOIN_FILTERS");
            foreach (JoinFilter filter in JoinFilters)
            {
                KONNode filterNode = new KONNode("FILTER");
                filterNode.AddValue("regex", filter.Regex.ToString());
                filterNode.AddValue("ban", filter.Ban);
                regexFiltersNode.AddChild(filterNode);
            }
            node.AddChild(regexFiltersNode);

            KONNode joinBlacklistsNode = new KONNode("JOIN_BLACKLISTS");
            foreach (JoinBlacklist blacklist in JoinBlacklists)
            {
                KONNode blacklistNode = new KONNode("BLACKLIST");
                blacklistNode.AddValue("username", blacklist.Username);
                blacklistNode.AddValue("ban", blacklist.Ban);
                joinBlacklistsNode.AddChild(blacklistNode);
            }
            node.AddChild(joinBlacklistsNode);

            File.WriteAllText($@"{Utils.localDataPath}/Guild_{Id}/Index.cb", SensitiveInformation.EncryptDataFile(KONWriter.Default.Write(node)));
        }

        public GuildUserData GetOrCreateUserData(ulong userId)
        {
            if (Users.ContainsKey(userId)) return Users[userId];
            GuildUserData output = new GuildUserData(userId, Id, true);
            Users.Add(userId, output);
            FlushData();
            return output;
        }

        public GuildData(ulong id, bool createIndex = false)
        {
            if (Program.doNotWrite) createIndex = false;
            else Directory.CreateDirectory($@"{Utils.localDataPath}/Guild_{id}"); //Make sure the directory exists to store data in
            Id = id;
            Users = new Dictionary<ulong, GuildUserData>();
            RolesToAssignOnJoin = new List<ulong>();
            JoinFilters = new List<JoinFilter>();
            JoinBlacklists = new List<JoinBlacklist>();
            GuildPrefix = "%";
            if (createIndex)
                FlushData();
        }
        public void AddJoinRole(ulong Id)
        {
            if (!RolesToAssignOnJoin.Contains(Id))
                RolesToAssignOnJoin.Add(Id);
        }
        public void RemoveJoinRole(ulong Id)
        {
            RolesToAssignOnJoin.RemoveAll(x => x.Equals(Id));
        }
    }

    public class JoinFilter
    {
        public Regex Regex { get; internal set; }
        public bool Ban { get; internal set; }

        public JoinFilter(string regex, bool ban)
        {
            Regex = new Regex(regex);
            Ban = ban;
        }

        public override string ToString()
        {
            return $"{Regex.ToString()} (" + (Ban ? "ban" : "kick") + ")";
        }
    }

    public class JoinBlacklist
    {
        public string Username { get; internal set; }
        public bool Ban { get; internal set; }

        public JoinBlacklist(string username, bool ban)
        {
            Username = username;
            Ban = ban;
        }

        public override string ToString()
        {
            return $"{Username} (" + (Ban ? "ban" : "kick") + ")";
        }
    }
}