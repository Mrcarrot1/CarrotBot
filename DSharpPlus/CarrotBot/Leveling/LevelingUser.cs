using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using DSharpPlus.Entities;
using KarrotObjectNotation;

namespace CarrotBot.Leveling
{
    public class LevelingUser
    {
        public ulong Id { get; }
        public LevelingServer Server { get; }
        public int Level { get; internal set; }
        public int CurrentXP { get; internal set; }
        public DateTimeOffset LastMessageTimestamp { get; internal set; }
        private TimeSpan messageInterval { get; set; }
        public bool MentionForLevelUp { get; internal set; }

        public int TotalXP
        {
            get
            {
                int output = CurrentXP;
                for (int i = Level; i > 0; i--)
                {
                    output += i * Server.XPPerLevel;
                }
                return output;
            }
        }

        public async Task HandleMessage(DiscordMessage msg)
        {
            if (Server.NoXPChannels.Contains(msg.ChannelId)) return;
            if (msg.MessageType == DSharpPlus.MessageType.GuildMemberJoin) return;
            messageInterval = new TimeSpan(0, 0, Server.XPCooldown);
            if (msg.Content.StartsWith(Data.Database.GetOrCreateGuildData((ulong)msg.Channel.GuildId).GuildPrefix) || (Program.isBeta && msg.Content.StartsWith("b%"))) return;
            if (DateTimeOffset.Now - LastMessageTimestamp > messageInterval)
            {
                if (msg.Channel.Guild.Id == 824824193001979924)
                {
                    if (Dripcoin.UserBalances.ContainsKey(msg.Author.Id))
                    {
                        Dripcoin.AddBalance(msg.Author.Id, 1);
                    }
                    else
                    {
                        Dripcoin.CreateUser(msg.Author.Id);
                        Dripcoin.AddBalance(msg.Author.Id, 1);
                    }
                }
                CurrentXP += Server.GetMessageXP();
                if (CurrentXP >= Server.XPNeededForLevel(Level + 1))
                {
                    Level += GetLevelIncrease(Level, CurrentXP, out int xpUsed);
                    CurrentXP -= xpUsed;
                    DiscordEmbedBuilder eb = new DiscordEmbedBuilder
                    {
                        Description = $"Congratulations <@{msg.Author.Id}> !\nYou have advanced to level **{Level}**!",
                        Title = "Level Up",
                        Color = Utils.CBOrange
                    };
                    if (Server.RoleRewards.ContainsKey(Level))
                    {
                        eb.Description += $"\nYou have unlocked the <@&{Server.RoleRewards[Level]}> role!";
                        DiscordGuild guild = msg.Channel.Guild;
                        DiscordMember member = await guild.GetMemberAsync(msg.Author.Id);
                        DiscordRole role = guild.GetRole(Server.RoleRewards[Level]);
                        await member.GrantRoleAsync(role);
                        if (!Server.CumulativeRoles)
                        {
                            if (Server.RoleRewards.Keys.Count > 1)
                            {
                                int index = Server.RoleRewards.Keys.ToList().IndexOf(Level);
                                if (index > 0)
                                {
                                    for (int i = index - 1; i >= 0; i--)
                                    {
                                        ulong roleId = Server.RoleRewards[Server.RoleRewards.Keys.ToArray()[i]];
                                        if (member.Roles.ToList().Any(x => x.Id == roleId))
                                        {
                                            await member.RevokeRoleAsync(guild.GetRole(roleId));
                                        } 
                                    }
                                }
                            }
                        }
                    }
                    if (Server.LevelUpMessages.ContainsKey(Level))
                    {
                        eb.Description += $"\n{Server.LevelUpMessages[Level]}";
                    }
                    Server.SortUsersByRank();
                    string content = MentionForLevelUp ? $"<@!{msg.Author.Id}>" : null;
                    if (Server.LevelUpChannel == null)
                        await msg.RespondAsync(content, eb.Build());
                    else
                        await msg.Channel.Guild.Channels[(ulong)Server.LevelUpChannel].SendMessageAsync(content, eb.Build());
                }
                LastMessageTimestamp = DateTimeOffset.Now;
                FlushData();
            }
        }
        public void FlushData()
        {
            if (Program.doNotWrite) return;
            KONNode node = new KONNode("LEVELING_USER");
            node.AddValue("id", Id);
            node.AddValue("xp", CurrentXP);
            node.AddValue("level", Level);
            node.AddValue("lastMessageTime", LastMessageTimestamp.ToUnixTimeSeconds());
            node.AddValue("mentionForLevelUp", MentionForLevelUp);
            File.WriteAllText($@"{Utils.levelingDataPath}/Server_{Server.Id}/User_{Id}.cb", SensitiveInformation.EncryptDataFile(KONWriter.Default.Write(node)));
        }
        public LevelingUser(ulong id, LevelingServer server)
        {
            Id = id;
            Level = 0;
            CurrentXP = 5; //The user is created after one message, which means 5 XP
            LastMessageTimestamp = DateTimeOffset.Now;
            Server = server;
            FlushData();
        }
        public LevelingUser(ulong id, int xp, int level, LevelingServer server)
        {
            Id = id;
            CurrentXP = xp;
            Level = level;
            Server = server;
            LastMessageTimestamp = DateTimeOffset.Now;
            FlushData();
        }
        public LevelingUser(ulong id, int xp, int level, LevelingServer server, DateTimeOffset lastMessageTime)
        {
            Id = id;
            CurrentXP = xp;
            Level = level;
            Server = server;
            LastMessageTimestamp = lastMessageTime;
            FlushData();
        }
        private int GetLevelIncrease(int startingLevel, int xp, out int xpUsed)
        {
            int output = 0;
            xpUsed = 0;
            while (xp >= Server.XPNeededForLevel(startingLevel + output + 1))
            {
                output++;
                xp -= Server.XPNeededForLevel(startingLevel + output);
                xpUsed += Server.XPNeededForLevel(startingLevel + output);
            }
            return output;
        }
    }
}