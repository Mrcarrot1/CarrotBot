using System;
using System.Collections.Generic;
using System.IO;
using KarrotObjectNotation;

namespace CarrotBot.Data
{
    public class GuildData
    {
        public ulong Id { get; set; }
        public Dictionary<ulong, GuildUserData> Users { get; internal set; }

        public List<ulong> RolesToAssignOnJoin { get; internal set; }

        public string GuildPrefix { get; internal set; }

        public List<System.Text.RegularExpressions.Regex> JoinFilters { get; internal set; }

        public List<string> JoinBlacklist { get; internal set; }

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

            KONArray regexFiltersArray = new KONArray("JOIN_FILTERS");
            foreach (System.Text.RegularExpressions.Regex regex in JoinFilters)
            {
                regexFiltersArray.AddItem(regex.ToString());
            }
            node.AddArray(regexFiltersArray);

            KONArray joinBlacklistArray = new KONArray("JOIN_BLACKLIST");
            foreach (string s in JoinBlacklist)
            {
                joinBlacklistArray.AddItem(s);
            }
            node.AddArray(joinBlacklistArray);

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
            JoinFilters = new List<System.Text.RegularExpressions.Regex>();
            JoinBlacklist = new List<string>();
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
}