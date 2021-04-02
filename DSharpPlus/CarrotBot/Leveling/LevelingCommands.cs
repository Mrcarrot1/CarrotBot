using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace CarrotBot.Leveling
{
    public class LevelingCommands
    {
        [Command("rank")]
        public async Task Rank(CommandContext ctx, string user = null)
        {
            try
            {
                DiscordMember Member = null;
                if(user == null)
                {
                    Member = ctx.Member;
                }
                else
                {
                    try
                    {
                        Member = await ctx.Guild.GetMemberAsync(Utils.GetId(user));
                    }
                    catch(FormatException)
                    {
                        Member = (await ctx.Guild.GetAllMembersAsync()).FirstOrDefault(x => x.Username == user);
                    }
                }
                LevelingServer lvlServer = LevelingData.Servers[ctx.Guild.Id];
                LevelingUser lvlUser = LevelingData.Servers[ctx.Guild.Id].Users[Member.Id];
                lvlServer.SortUsersByRank();
                DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
                eb.WithTitle($"{Member.Username}'s Level");
                eb.WithThumbnailUrl(Member.AvatarUrl);
                //This looks bad
                //and it is
                eb.WithDescription($"Level **{lvlUser.Level}**\n{lvlUser.CurrentXP}/{LevelingData.XPNeededForLevel(lvlUser.Level + 1)} XP\nServer rank: **{lvlServer.UsersByRank.IndexOf(lvlServer.UsersByRank.FirstOrDefault(x => x.Id == lvlUser.Id)) + 1}/{lvlServer.UsersByRank.Count}**");
                //eb.WithFooter("This is a beta. Server rankings are coming soon.");
                await ctx.RespondAsync(embed: eb.Build());
            }
            catch
            {
                if(user == null)
                    await ctx.RespondAsync("Either this server hasn't set up ranking, or you aren't ranked!");
                else
                    await ctx.RespondAsync("Either this server hasn't set up ranking, I can't find that user, or that user isn't ranked!");
            }
        }
        [Command("addlevelrole")]
        public async Task AddLevelRole(CommandContext ctx, int level, string role)
        {
            try
            {
                DiscordRole Role = null;
                try
                {
                    Role = ctx.Guild.GetRole(Utils.GetId(role));
                }
                catch(FormatException)
                {
                    Role = ctx.Guild.Roles.FirstOrDefault(x => x.Name == role);
                }
                LevelingData.Servers[ctx.Guild.Id].RoleRewards.Add(level, Role.Id);
                LevelingData.Servers[ctx.Guild.Id].FlushData();
                await ctx.RespondAsync($"Added role for level {level}: {Role.Name}");
            }
            catch
            {
                await ctx.RespondAsync("Either this server doesn't have leveling set up or I can't find that role!");
            }
        }
        [Command("leaderboard")]
        public async Task Leaderboard(CommandContext ctx)
        {
            LevelingServer levelingServer = LevelingData.Servers[ctx.Guild.Id];
            levelingServer.SortUsersByRank();
            int usersToShow = levelingServer.UsersByRank.Count < 20 ? levelingServer.UsersByRank.Count : 20;
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder
            {
                Title = $"{ctx.Guild.Name} Leveling Leaderboard",
                Description = $"**Showing the top {usersToShow} users**"
            };
            for(int i = 0; i < usersToShow; i++)
            {
                LevelingUser user = levelingServer.UsersByRank[i];
                eb.Description += $"\n**{i + 1}.** <@!{user.Id}> | Level **{user.Level}** | {user.CurrentXP}/{LevelingData.XPNeededForLevel(user.Level + 1)} XP";
            }
            await ctx.RespondAsync(embed: eb.Build());
        }
    }
}