using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DSharpPlus.SlashCommands;
using KarrotObjectNotation;

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

        public ulong? ModMailChannel { get; internal set; }

        public ulong? MessageLogsChannel { get; internal set; }
        
        public Dictionary<ulong, ulong> CustomRoles { get; }

        public AllowCustomRoles CustomRolesAllowed { get; internal set; }

        public void FlushData(bool flushUserData = false)
        {
            if (Program.doNotWrite) return;
            KONNode node = new($"GUILD_{Id}");
            node.AddValue("id", Id);
            node.AddValue("prefix", GuildPrefix);
            if (ModMailChannel != null)
            {
                node.AddValue("modmailChannel", (ulong)ModMailChannel);
            }
            if (MessageLogsChannel != null)
            {
                node.AddValue("messageLogsChannel", (ulong)MessageLogsChannel);
            }

            node.AddValue("customRolesAllowed", CustomRolesAllowed.ToString());

            KONArray usersArray = new("USERS");
            foreach (KeyValuePair<ulong, GuildUserData> user in Users)
            {
                usersArray.Items.Add(user.Key);
                if (flushUserData) user.Value.FlushData();
            }
            node.AddArray(usersArray);

            KONArray joinRolesArray = new("JOIN_ROLES");
            foreach (ulong role in RolesToAssignOnJoin)
            {
                joinRolesArray.Items.Add(role);
            }
            node.AddArray(joinRolesArray);

            KONNode regexFiltersNode = new("JOIN_FILTERS");
            foreach (JoinFilter filter in JoinFilters)
            {
                KONNode filterNode = new("FILTER");
                filterNode.AddValue("regex", filter.Regex.ToString());
                filterNode.AddValue("ban", filter.Ban);
                filterNode.AddValue("creatorId", filter.CreatorId);
                KONArray exceptionsArray = new("EXCEPTIONS");
                foreach (ulong exception in filter.Exceptions)
                {
                    exceptionsArray.AddItem(exception);
                }
                filterNode.AddArray(exceptionsArray);
                regexFiltersNode.AddChild(filterNode);
            }
            node.AddChild(regexFiltersNode);

            KONNode joinBlacklistsNode = new("JOIN_BLACKLISTS");
            foreach (JoinBlacklist blacklist in JoinBlacklists)
            {
                KONNode blacklistNode = new("BLACKLIST");
                blacklistNode.AddValue("username", blacklist.Username);
                blacklistNode.AddValue("ban", blacklist.Ban);
                blacklistNode.AddValue("creatorId", blacklist.CreatorId);
                KONArray exceptionsArray = new("EXCEPTIONS");
                foreach (ulong exception in blacklist.Exceptions)
                {
                    exceptionsArray.AddItem(exception);
                }
                blacklistNode.AddArray(exceptionsArray);
                joinBlacklistsNode.AddChild(blacklistNode);
            }
            node.AddChild(joinBlacklistsNode);

            KONNode customRolesNode = new("CUSTOM_ROLES");
            foreach (KeyValuePair<ulong, ulong> role in CustomRoles)
            {
                KONNode roleNode = new("ROLE");
                roleNode.AddValue("user", role.Key);
                roleNode.AddValue("role", role.Value);
                customRolesNode.AddChild(roleNode);
            }
            node.AddChild(customRolesNode);
            
            SensitiveInformation.AES256WriteFile($@"{Utils.localDataPath}/Guild_{Id}/Index.cb", KONWriter.Default.Write(node));
        }

        public GuildUserData GetOrCreateUserData(ulong userId)
        {
            if (Users.TryGetValue(userId, out var data)) return data;
            GuildUserData output = new(userId, Id, true);
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
            CustomRolesAllowed = AllowCustomRoles.None;
            CustomRoles = new();
            if (createIndex)
                FlushData();
        }
        public void AddJoinRole(ulong id)
        {
            if (!RolesToAssignOnJoin.Contains(id))
                RolesToAssignOnJoin.Add(id);
        }
        public void RemoveJoinRole(ulong id)
        {
            RolesToAssignOnJoin.RemoveAll(x => x.Equals(id));
        }

        public enum AllowCustomRoles
        {
            [ChoiceName("None")]
            None,
            [ChoiceName("Boosters Only")]
            Booster,
            [ChoiceName("All Members")]
            All
        }
    }

    public class JoinFilter
    {
        public Regex Regex { get; internal set; }
        public bool Ban { get; internal set; }
        public ulong CreatorId { get; internal set; }
        public List<ulong> Exceptions { get; internal set; }

        public JoinFilter(string regex, bool ban, ulong creatorId)
        {
            Regex = new Regex(regex);
            Ban = ban;
            CreatorId = creatorId;
            Exceptions = new();
        }

        public override string ToString()
        {
            return $"{Regex} {(Ban ? "ban" : "kick")}";
        }
    }

    public class JoinBlacklist
    {
        public string Username { get; internal set; }
        public bool Ban { get; internal set; }
        public ulong CreatorId { get; internal set; }
        public List<ulong> Exceptions { get; internal set; }

        public JoinBlacklist(string username, bool ban, ulong creatorId)
        {
            Username = username;
            Ban = ban;
            CreatorId = creatorId;
            Exceptions = new();
        }

        public override string ToString()
        {
            return $"{Username} (" + (Ban ? "ban" : "kick") + ")";
        }
    }
}