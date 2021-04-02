using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using DSharpPlus.Entities;

namespace CarrotBot.Leveling
{
    public class LevelingUser
    {
        public ulong Id { get; }
        public LevelingServer Server { get; }
        public int Level { get; internal set; }
        public int CurrentXP { get; internal set; }
        public DateTimeOffset LastMessageTimestamp { get; internal set; }
        private TimeSpan messageInterval = new TimeSpan(0, 0, 60);

        public async Task HandleMessage(DiscordMessage msg)
        {
            if(msg.Content.StartsWith('%')) return;
            if(DateTimeOffset.Now - LastMessageTimestamp > messageInterval)
            {
                CurrentXP += 5;
                if(CurrentXP >= LevelingData.XPNeededForLevel(Level + 1))
                {
                    Level++;
                    CurrentXP = 0;
                    DiscordEmbedBuilder eb =  new DiscordEmbedBuilder
                    {
                        Description = $"Congratulations <@{msg.Author.Id}> !\nYou have advanced to level **{Level}**!",
                        Title = "Level Up",
                        Color = DiscordColor.Lilac,
                        ThumbnailUrl = msg.Author.AvatarUrl
                    };
                    if(Server.RoleRewards.ContainsKey(Level))
                    {
                        eb.Description += $"\nYou have unlocked the <@&{Server.RoleRewards[Level]}> role!";
                        await msg.Channel.Guild.GetMemberAsync(msg.Author.Id).Result.GrantRoleAsync(msg.Channel.Guild.GetRole(Server.RoleRewards[Level]));
                    }
                    Server.SortUsersByRank();
                    await msg.RespondAsync(embed: eb.Build());
                }
                LastMessageTimestamp = DateTimeOffset.Now;
                FlushData();
            }
        }
        public void FlushData()
        {
            ConfigNode node = new ConfigNode("LEVELING_USER");
            node.Values.Add("id", Id.ToString());
            node.Values.Add("xp", CurrentXP.ToString());
            node.Values.Add("level", Level.ToString());
            node.Values.Add("lastMessageTime", LastMessageTimestamp.ToUnixTimeSeconds().ToString());
            File.WriteAllText($@"{Utils.levelingDataPath}/Server_{Server.Id}/User_{Id}.cb", ConfigWriter.Write(node));
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
    }
}