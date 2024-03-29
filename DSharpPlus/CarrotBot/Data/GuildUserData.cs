using System;
using System.Collections.Generic;
using System.IO;
using DSharpPlus;
using DSharpPlus.Entities;
using KarrotObjectNotation;

namespace CarrotBot.Data
{
    public class GuildUserData
    {
        public ulong Id { get; }
        public bool IsAFK { get; internal set; }
        public string AFKMessage { get; internal set; }
        public DateTimeOffset AFKTime { get; internal set; }
        public List<Tuple<string, DateTimeOffset, ulong>> Warnings { get; internal set; } //Warning message, time, user who issued the warning
        public ulong GuildId { get; }

        public void SetAFK(string message)
        {
            IsAFK = true;
            AFKMessage = message;
            AFKTime = DateTimeOffset.Now;
            FlushData();
        }
        public void RemoveAFK()
        {
            IsAFK = false;
            AFKMessage = null;
            FlushData();
        }

        public void AddWarning(string message, ulong moderator)
        {
            Warnings.Add(new Tuple<string, DateTimeOffset, ulong>(message, DateTimeOffset.Now, moderator));
        }

        public void FlushData()
        {
            if (Program.doNotWrite) return;
            KONNode userNode = new KONNode($"GUILD_{GuildId}_USER_{Id}");
            userNode.AddValue("id", Id);
            userNode.AddValue("isAFK", IsAFK);
            if (IsAFK)
            {
                userNode.AddValue("AFKTime", AFKTime.ToUnixTimeMilliseconds());
                userNode.AddValue("AFKMessage", AFKMessage);
            }
            foreach (var warning in Warnings)
            {
                KONNode warningNode = new KONNode("WARNING");
                warningNode.AddValue("message", warning.Item1);
                warningNode.AddValue("time", warning.Item2.ToUnixTimeMilliseconds());
                warningNode.AddValue("warnedBy", warning.Item3);
                userNode.AddChild(warningNode);
            }
            File.WriteAllText($@"{Utils.localDataPath}/Guild_{GuildId}/User_{Id}.cb", SensitiveInformation.EncryptDataFile(KONWriter.Default.Write(userNode)));
        }

        public GuildUserData(ulong id, ulong guildId, bool createFile = false)
        {
            Id = id;
            GuildId = guildId;
            IsAFK = false;
            AFKMessage = null;
            Warnings = new List<Tuple<string, DateTimeOffset, ulong>>();
            if (createFile)
                FlushData();
        }
    }
}