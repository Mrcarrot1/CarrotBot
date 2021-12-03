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
    public class LevelingCommands : BaseCommandModule
    {
        [Command("rank"), Aliases("level"), Description("Shows your level and rank in the server."), LevelingCommandAttribute]
        public async Task Rank(CommandContext ctx, [Description("The user to check the rank for.")]string user = null)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
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
                        Member = await ctx.Guild.FindMemberAsync(user);
                    }
                    catch
                    {
                        await ctx.RespondAsync("I couldn't find that user. Try making sure they're in the server?");
                    }
                }
                LevelingServer lvlServer = LevelingData.Servers[ctx.Guild.Id];
                LevelingUser lvlUser = LevelingData.Servers[ctx.Guild.Id].Users[Member.Id];
                lvlServer.SortUsersByRank();
                DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
                eb.WithTitle($"{Member.Username}'s Level");
                eb.WithThumbnail(Member.AvatarUrl);
                eb.WithColor(Utils.CBOrange);
                //This looks bad
                //and it is
                eb.WithDescription($"Level **{lvlUser.Level}**\n{lvlUser.CurrentXP}/{lvlServer.XPNeededForLevel(lvlUser.Level + 1)} XP\nServer rank: **{lvlServer.UsersByRank.IndexOf(lvlServer.UsersByRank.FirstOrDefault(x => x.Id == lvlUser.Id)) + 1}/{lvlServer.UsersByRank.Count}**");
                //eb.WithFooter("This is a beta. Server rankings are coming soon.");
                await ctx.RespondAsync(embed: eb.Build());
            }
            catch
            {
                if(user == null)
                    await ctx.RespondAsync("Either this server hasn't set up ranking, or you aren't ranked!");
                else
                    await ctx.RespondAsync("Either this server hasn't set up ranking, or that user isn't ranked!");
            }
        }
        [Command("addlevelrole"), RequirePermissions(Permissions.ManageRoles), Description("Adds a role to be granted to a user when they reach a certain level."), LevelingCommandAttribute, RequireLeveling]
        public async Task AddLevelRole(CommandContext ctx, [Description("The level at which to grant the role.")]int level, [Description("The role to grant.")]string role)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
            try
            {
                DiscordRole Role = null;
                try
                {
                    Role = ctx.Guild.GetRole(Utils.GetId(role));
                }
                catch(FormatException)
                {
                    Role = ctx.Guild.Roles.FirstOrDefault(x => x.Value.Name == role).Value;
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
        [Command("removelevelrole"), RequirePermissions(Permissions.ManageRoles), Description("Removes a level from being granted at a certain level."), LevelingCommandAttribute, RequireLeveling]
        public async Task RemoveLevelRole(CommandContext ctx, [Description("The level from which to remove the role.")]int level)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
            try
            {
                string roleName = "";
                if(LevelingData.Servers[ctx.Guild.Id].RoleRewards.ContainsKey(level))
                {
                    if(ctx.Guild.Roles.ContainsKey(LevelingData.Servers[ctx.Guild.Id].RoleRewards[level]))
                        roleName = ctx.Guild.Roles[LevelingData.Servers[ctx.Guild.Id].RoleRewards[level]].Name;
                    LevelingData.Servers[ctx.Guild.Id].RoleRewards.Remove(level);
                }
                LevelingData.Servers[ctx.Guild.Id].FlushData();
                await ctx.RespondAsync($"Removed role from level {level}: {roleName}");
            }
            catch
            {
                await ctx.RespondAsync("Either this server doesn't have leveling set up or I can't find that role!");
            }
        }
        [Command("leaderboard"), Aliases("lb", "top"), Description("Shows a leaderboard of users"), LevelingCommandAttribute]
        public async Task Leaderboard(CommandContext ctx, int page = 1)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
            if(page < 1)
            {
                await ctx.RespondAsync("Invalid page number!");
            }
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
                    eb.Description += $"\n**{i + 1}.** \t<@!{user.Id}> | Level **{user.Level}** | {user.CurrentXP}/{levelingServer.XPNeededForLevel(user.Level + 1)} XP";
                }
                eb.WithColor(Utils.CBOrange);
                await ctx.RespondAsync(embed: eb.Build());
            }
            else
            {
                DiscordEmbedBuilder eb = new DiscordEmbedBuilder
                {
                    Title = $"{ctx.Guild.Name} Leveling Leaderboard",
                    Description = $"**Showing page {page}**"
                };
                if(levelingServer.UsersByRank.Count < 20 * (int)(page - 1))
                {
                    await ctx.RespondAsync("No users to show on that page!");
                }
                int usersToShow = levelingServer.UsersByRank.Count - (page - 1 * 20) < 20 ? levelingServer.UsersByRank.Count - (page - 1 * 20) : 20;
                for(int i = (20 * (page - 1)); i < (20 * (page - 1)) + 20; i++)
                {
                    LevelingUser user = levelingServer.UsersByRank[i];
                    eb.Description += $"\n**{i + 1},** \t<@!{user.Id}> | Level **{user.Level}** | {user.CurrentXP}/{levelingServer.XPNeededForLevel(user.Level + 1)} XP";
                }
            }
        }
        [Command("enableleveling"), RequireUserPermissions(Permissions.ManageGuild), LevelingCommandAttribute, RequireLeveling(false)]
        public async Task EnableLeveling(CommandContext ctx)
        {
            if(LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is already enabled for this server.\nUse `%disableleveling` if you wish to disable it.");
                return;
            }
            LevelingData.AddServer(ctx.Guild.Id);
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle("Leveling Enabled");
            eb.WithDescription("You have enabled leveling for this server. Here are some tips to help you customize the experience!");
            eb.AddField("Useful Commands", $"`addnoxpchannel`: Blacklists a channel from earning XP. Useful for hidden channels and/or places such as meme chats or bot channels.\n`setlevelupchannel`: Redirects level-up messages to a specific channel. Can be used to keep chat cleaner.\n`addlevelrole`: Adds a role reward for users who reach a certain level.\nSee `{Data.Database.GetOrCreateGuildData(ctx.Guild.Id).GuildPrefix}help leveling` for a full list of commands.");
            await ctx.RespondAsync(embed: eb.Build());
        }
        [Command("disableleveling"), RequireUserPermissions(Permissions.ManageGuild), LevelingCommandAttribute, RequireLeveling]
        public async Task DisableLeveling(CommandContext ctx, [Description("Whether or not to delete all leveling data for this server")] bool deleteData)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
            LevelingData.RemoveServer(ctx.Guild.Id, deleteData);
        }
        [Command("addnoxpchannel"), RequirePermissions(Permissions.ManageGuild), Description("Blacklists a channel from earning XP."), LevelingCommandAttribute, RequireLeveling]
        public async Task AddNoXPChannel(CommandContext ctx, [RemainingText] string channel)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
            ulong channelId = 0;
            try
            {
                channelId = Utils.GetId(channel);
            }
            catch(FormatException)
            {
                try
                {
                    channelId = ctx.Guild.Channels.FirstOrDefault(x => x.Value.Name == channel).Key;
                }
                catch
                {
                    await ctx.RespondAsync("Channel not found!");
                    return;
                }
            }
            LevelingData.Servers[ctx.Guild.Id].NoXPChannels.Add(channelId);
            await ctx.RespondAsync($"Added XP-blocked channel: <#{channelId}>");
        }
        [Command("removenoxpchannel"), RequirePermissions(Permissions.ManageGuild), Description("Removes a channel from the XP blacklist."), LevelingCommandAttribute, RequireLeveling]
        public async Task RemoveNoXPChannel(CommandContext ctx, [RemainingText] string channel)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
            ulong channelId = 0;
            try
            {
                channelId = Utils.GetId(channel);
            }
            catch(FormatException)
            {
                try
                {
                    channelId = ctx.Guild.Channels.FirstOrDefault(x => x.Value.Name == channel).Key;
                }
                catch
                {
                    await ctx.RespondAsync("Channel not found!");
                    return;
                }
            }
            LevelingData.Servers[ctx.Guild.Id].NoXPChannels.RemoveAll(x => x == channelId);
            await ctx.RespondAsync($"Removed XP-blocked channel: <#{channelId}>");
        }
        [Command("setlevelupchannel"), RequirePermissions(Permissions.ManageGuild), Description("Sets a channel to send all level-up messages in."), LevelingCommandAttribute, RequireLeveling]
        public async Task SetLevelUpChannel(CommandContext ctx, [RemainingText] string channel)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
            ulong channelId = 0;
            try
            {
                channelId = Utils.GetId(channel);
            }
            catch(FormatException)
            {
                try
                {
                    channelId = ctx.Guild.Channels.FirstOrDefault(x => x.Value.Name == channel).Key;
                }
                catch
                {
                    await ctx.RespondAsync("Channel not found!");
                    return;
                }
            }
            LevelingData.Servers[ctx.Guild.Id].SetLevelUpChannel(channelId);
            LevelingData.Servers[ctx.Guild.Id].FlushData();
            await ctx.RespondAsync($"Set level-up message channel: <#{channelId}>");
        }
        [Command("removelevelupchannel"), Description("Removes the channel all level-up messages are being sent to."), RequirePermissions(Permissions.ManageGuild), LevelingCommandAttribute, RequireLeveling]
        public async Task RemoveLevelUpChannel(CommandContext ctx)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
            LevelingData.Servers[ctx.Guild.Id].SetLevelUpChannel(null);
            LevelingData.Servers[ctx.Guild.Id].FlushData();
            await ctx.RespondAsync("Removed level-up message channel.");
        }
        [Command("addlevelupmessage"), Description("Adds a message that will be displayed to a user who reaches a certain level."), RequireUserPermissions(Permissions.ManageGuild), LevelingCommandAttribute, RequireLeveling]
        public async Task AddLevelUpMessage(CommandContext ctx, [Description("The level to show the message at.")]int level, [RemainingText, Description("The message to show.")]string message)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
            if(!LevelingData.Servers[ctx.Guild.Id].LevelUpMessages.ContainsKey(level))
                LevelingData.Servers[ctx.Guild.Id].LevelUpMessages.Add(level, message);
            else
                LevelingData.Servers[ctx.Guild.Id].LevelUpMessages[level] = message;
            await ctx.RespondAsync($"Set message for level {level}.");
            LevelingData.Servers[ctx.Guild.Id].FlushData();
        }
        [Command("removelevelupmessage"), Description("Removes a level up message from the list to be shown to users who reach a certain level."), RequireUserPermissions(Permissions.ManageGuild), LevelingCommandAttribute, RequireLeveling]
        public async Task RemoveLevelUpMessage(CommandContext ctx, [Description("The level to remove the message from.")]int level)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
            if(LevelingData.Servers[ctx.Guild.Id].LevelUpMessages.ContainsKey(level))
            {
                LevelingData.Servers[ctx.Guild.Id].LevelUpMessages.Remove(level);
            }
            await ctx.RespondAsync($"Removed level up message for level {level}.");
            LevelingData.Servers[ctx.Guild.Id].FlushData();
        }
        [Command("setxpcooldown"), Description("Sets the XP cooldown in this server."), RequireUserPermissions(Permissions.ManageGuild), LevelingCommandAttribute, RequireLeveling]
        public async Task SetXPCooldown(CommandContext ctx, [Description("The cooldown in seconds. Default: 60.")] int cooldown)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
            if(cooldown < 0)
            {
                await ctx.RespondAsync("Invalid cooldown period!");
            }
            else
            {
                LevelingData.Servers[ctx.Guild.Id].XPCooldown = cooldown;
                await ctx.RespondAsync($"Set XP cooldown to **{cooldown}** seconds.");
            }
            LevelingData.Servers[ctx.Guild.Id].FlushData();
        }
        [Command("setxpperlevel"), Description("Sets the XP per level in this server."), RequireUserPermissions(Permissions.ManageGuild), LevelingCommandAttribute, RequireLeveling]
        public async Task SetXPPerLevel(CommandContext ctx, [Description("The amount of XP required to reach level 1. Default: 150.")] int xp)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
            if(xp < 1)
            {
                await ctx.RespondAsync("Invalid XP count!");
            }
            else
            {
                LevelingData.Servers[ctx.Guild.Id].XPPerLevel = xp;
                await ctx.RespondAsync($"Set XP per level to **{xp}**.");
            }
            LevelingData.Servers[ctx.Guild.Id].FlushData();
        }
        [Command("setxppermessage"), Description("Sets the XP a user receives for sending one message."), RequireUserPermissions(Permissions.ManageGuild), LevelingCommandAttribute, RequireLeveling]
        public async Task SetXPPerMessage(CommandContext ctx, [Description("The amount of XP to give. Default: 5.")] int xp)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
            if(xp < 1)
            {
                await ctx.RespondAsync("Invalid XP value!");
            }
            else
            {
                LevelingData.Servers[ctx.Guild.Id].MinXPPerMessage = xp;
                LevelingData.Servers[ctx.Guild.Id].MaxXPPerMessage = xp;
                await ctx.RespondAsync($"Set xp per message to **{xp}**.");
            }
            LevelingData.Servers[ctx.Guild.Id].FlushData();
        }
        [Command("setxppermessage"), Description("Sets the XP a user receives for sending one message."), RequireUserPermissions(Permissions.ManageGuild), LevelingCommandAttribute, RequireLeveling]
        public async Task SetXPPerMessage(CommandContext ctx, [Description("The minimum amount of XP to give. Default: 5.")] int minXP, [Description("The maximum amount of XP to give. Default: 5.")] int maxXP)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
            if(minXP < 0 || maxXP < minXP || maxXP < 1)
            {
                await ctx.RespondAsync("Invalid XP values!");
            }
            else
            {
                LevelingData.Servers[ctx.Guild.Id].MinXPPerMessage = minXP;
                LevelingData.Servers[ctx.Guild.Id].MaxXPPerMessage = maxXP;
                await ctx.RespondAsync($"Set xp per message to a random value from **{minXP}** to **{maxXP}**.");
            }
            LevelingData.Servers[ctx.Guild.Id].FlushData();
        }
        [Command("levelingsettings"), Description("Shows the settings for leveling in this server."), LevelingCommandAttribute, RequireLeveling]
        public async Task LevelingSettings(CommandContext ctx)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
            LevelingServer lvlServer = LevelingData.Servers[ctx.Guild.Id];
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle($"Leveling Settings for {ctx.Guild.Name}");
            if(lvlServer.LevelUpChannel != null)
                eb.AddField("Level-Up Channel", $"<#{lvlServer.LevelUpChannel}>");
            eb.AddField("XP Cooldown", $"{lvlServer.XPCooldown}");
            eb.AddField("XP Per Level", $"{lvlServer.XPPerLevel}", true);
            eb.AddField("Min XP Per Message", $"{lvlServer.MinXPPerMessage}");
            eb.AddField("Max XP Per Message", $"{lvlServer.MaxXPPerMessage}", true);
            eb.WithColor(Utils.CBOrange);
            await ctx.RespondAsync(eb.Build());
        }
        //[Command("resetleveling"), Description("Resets all leveling settings in this server to the defaults. Also resets XP and levels."), RequireUserPermissions(Permissions.ManageGuild), LevelingCommandAttribute, RequireLeveling]
        public async Task ResetLeveling(CommandContext ctx, bool confirm = false)
        {
            if(!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `%enableleveling` if you wish to enable it.");
                return;
            }
            if(confirm)
            {
                
            }
        }
    }
}