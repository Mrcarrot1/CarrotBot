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
        [Command("rank"), Aliases("level")]
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
        [Command("addlevelrole"), RequirePermissions(Permissions.ManageRoles)]
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
        [Command("leaderboard"), Aliases("lb")]
        public async Task Leaderboard(CommandContext ctx, uint page = 1)
        {
            LevelingServer levelingServer = LevelingData.Servers[ctx.Guild.Id];
            levelingServer.SortUsersByRank();
            if(page == 1)
            {
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
            else
            {
                if(levelingServer.UsersByRank.Count < 20 * (int)(page - 1))
                {
                    await ctx.RespondAsync("No users to show on that page!");
                }
                int usersToShow = levelingServer.UsersByRank.Count - (int)(page - 1 * 20) < 20 ? levelingServer.UsersByRank.Count - (int)(page - 1 * 20) : 20;
            }
        }
        [Command("enableleveling"), RequireUserPermissions(Permissions.ManageGuild)]
        public async Task EnableLeveling(CommandContext ctx)
        {
            if(LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is already enabled for this server.\nUse `%disableleveling` if you wish to disable it.");
                return;
            }
            LevelingData.AddServer(ctx.Guild.Id);
            await ctx.RespondAsync("Leveling has been enabled for this server.");
        }
        [Command("disableleveling"), RequireUserPermissions(Permissions.ManageGuild)]
        public async Task DisableLeveling(CommandContext ctx, [Description("Whether or not to delete all leveling data for this server")] bool deleteData)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
            LevelingData.RemoveServer(ctx.Guild.Id, deleteData);
        }
        [Command("addnoxpchannel"), RequirePermissions(Permissions.ManageGuild)]
        public async Task AddNoXPChannel(CommandContext ctx, [RemainingText] string channel)
        {
            ulong channelId = 0;
            try
            {
                channelId = Utils.GetId(channel);
            }
            catch(FormatException)
            {
                try
                {
                    channelId = ctx.Guild.Channels.FirstOrDefault(x => x.Name == channel).Id;
                }
                catch
                {
                    await ctx.RespondAsync("Channel not found!");
                    return;
                }
            }
            LevelingData.Servers[ctx.Guild.Id].NoXPChannels.Add(channelId);
            await ctx.RespondAsync("Added ");
        }
    }
}