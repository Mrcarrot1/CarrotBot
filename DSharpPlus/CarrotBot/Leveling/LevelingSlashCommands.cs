using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarrotBot.Data;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace CarrotBot.Leveling;

public class LevelingSlashCommands : ApplicationCommandModule
{
    [SlashCommand("rank", "Shows your level and rank in the server."), LevelingCommand, SlashRequireGuild]
    public async Task Rank(InteractionContext ctx, [Option("user", "The user to check the rank for.")] DiscordUser? user = null)
    {
        await ctx.IndicateResponseAsync();
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.UpdateResponseAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        try
        {
            DiscordMember? Member = null;
            if (user == null)
            {
                Member = ctx.Member;
            }
            else
            {
                try
                {
                    Member = user as DiscordMember;
                }
                catch
                {
                    await ctx.UpdateResponseAsync("I couldn't find that user. Try making sure they're in the server?");
                }
            }
            LevelingServer lvlServer = LevelingData.Servers[ctx.Guild.Id];
            LevelingUser lvlUser = LevelingData.Servers[ctx.Guild.Id].Users[Member!.Id];
            lvlServer.SortUsersByRank();
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle($"{Member.Username}'s Level");
            eb.WithThumbnail(Member.AvatarUrl);
            eb.WithColor(Utils.CBOrange);
            //This looks bad
            //and it is
            eb.WithDescription($"Level **{lvlUser.Level}**\n{lvlUser.CurrentXP}/{lvlServer.XPNeededForLevel(lvlUser.Level + 1)} XP\nServer rank: **{lvlServer.UsersByRank.IndexOf(lvlServer.UsersByRank.FirstOrDefault(x => x.Id == lvlUser.Id)!) + 1}/{lvlServer.UsersByRank.Count}**");
            //eb.WithFooter("This is a beta. Server rankings are coming soon.");
            await ctx.UpdateResponseAsync(embed: eb.Build());
        }
        catch
        {
            if (user == null)
                await ctx.UpdateResponseAsync("Either this server hasn't set up leveling, or you aren't ranked!");
            else
                await ctx.UpdateResponseAsync("Either this server hasn't set up leveling, or that user isn't ranked!");
        }
    }

    [SlashCommand("add-level-role", "Adds a role to be granted to a user when they reach a certain level.", false), SlashRequirePermissions(Permissions.ManageRoles), LevelingCommand, RequireLeveling, SlashRequireGuild]
    public async Task AddLevelRole(InteractionContext ctx, [Option("level", "The level at which to grant the role.")] long levell, [Option("role", "The role to grant.")] DiscordRole Role)
    {
        await ctx.IndicateResponseAsync();
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.UpdateResponseAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        try
        {
            if (levell > int.MaxValue || levell < 1)
            {
                await ctx.UpdateResponseAsync("Invalid level!");
                return;
            }
            int level = (int)levell;
            LevelingData.Servers[ctx.Guild.Id].RoleRewards.Add(level, Role.Id);
            LevelingData.Servers[ctx.Guild.Id].FlushData();
            await ctx.UpdateResponseAsync($"Added role for level {level}: {Role.Name}");
        }
        catch
        {
            await ctx.UpdateResponseAsync("Either this server doesn't have leveling set up or I can't find that role!");
        }
    }

    [SlashCommand("remove-level-role", "Removes a level from being granted at a certain level."), SlashRequirePermissions(Permissions.ManageRoles), LevelingCommand, RequireLeveling, SlashRequireGuild]
    public async Task RemoveLevelRole(InteractionContext ctx, [Option("level", "The level from which to remove the role.")] long levell)
    {
        await ctx.IndicateResponseAsync();
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.UpdateResponseAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        try
        {
            if (levell > int.MaxValue || levell < 1)
            {
                await ctx.UpdateResponseAsync("Invalid level!");
                return;
            }
            int level = (int)levell;
            string roleName = "";
            if (LevelingData.Servers[ctx.Guild.Id].RoleRewards.ContainsKey(level))
            {
                if (ctx.Guild.Roles.ContainsKey(LevelingData.Servers[ctx.Guild.Id].RoleRewards[level]))
                    roleName = ctx.Guild.Roles[LevelingData.Servers[ctx.Guild.Id].RoleRewards[level]].Name;
                LevelingData.Servers[ctx.Guild.Id].RoleRewards.Remove(level);
            }
            LevelingData.Servers[ctx.Guild.Id].FlushData();
            await ctx.UpdateResponseAsync($"Removed role from level {level}: {roleName}");
        }
        catch
        {
            await ctx.UpdateResponseAsync("Either this server doesn't have leveling set up or I can't find that role!");
        }
    }

    [SlashCommand("leaderboard", "Shows a leaderboard of users"), LevelingCommand, SlashRequireGuild]
    public async Task Leaderboard(InteractionContext ctx, [Option("page", "The page of the list to show.")] long pagel = 1)
    {
        await ctx.IndicateResponseAsync();
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.UpdateResponseAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        if (pagel > int.MaxValue || pagel < 1)
        {
            await ctx.UpdateResponseAsync("Invalid page!");
            return;
        }
        int page = (int)pagel;
        LevelingServer levelingServer = LevelingData.Servers[ctx.Guild.Id];
        levelingServer.SortUsersByRank();
        if (page == 1)
        {
            int usersToShow = levelingServer.UsersByRank.Count < 20 ? levelingServer.UsersByRank.Count : 20;
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder
            {
                Title = $"{ctx.Guild.Name} Leveling Leaderboard",
                Description = $"**Showing the top {usersToShow} users**"
            };
            for (int i = 0; i < usersToShow; i++)
            {
                LevelingUser user = levelingServer.UsersByRank[i];
                eb.Description += $"\n**{i + 1}.** \t<@!{user.Id}> | Level **{user.Level}** | {user.CurrentXP}/{levelingServer.XPNeededForLevel(user.Level + 1)} XP";
            }
            eb.WithColor(Utils.CBOrange);
            await ctx.UpdateResponseAsync(embed: eb.Build());
        }
        else
        {
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder
            {
                Title = $"{ctx.Guild.Name} Leveling Leaderboard",
                Description = $"**Showing page {page}**"
            };
            if (levelingServer.UsersByRank.Count < 20 * (page - 1))
            {
                await ctx.UpdateResponseAsync("No users to show on that page!");
            }
            //int usersToShow = levelingServer.UsersByRank.Count - (page - 1 * 20) < 20 ? levelingServer.UsersByRank.Count - (page - 1 * 20) : 20;
            for (int i = (20 * (page - 1)); i < (20 * (page - 1)) + 20; i++)
            {
                LevelingUser user = levelingServer.UsersByRank[i];
                eb.Description += $"\n**{i + 1}.** \t<@!{user.Id}> | Level **{user.Level}** | {user.CurrentXP}/{levelingServer.XPNeededForLevel(user.Level + 1)} XP";
            }
            eb.WithColor(Utils.CBOrange);
            await ctx.UpdateResponseAsync(embed: eb.Build());
        }
    }

    [SlashCommand("enable-leveling", "Enables leveling in this server.", false), SlashRequireUserPermissions(Permissions.ManageGuild), LevelingCommand, RequireLeveling(false), SlashRequireGuild]
    public async Task EnableLeveling(InteractionContext ctx)
    {
        await ctx.IndicateResponseAsync();
        if (LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.UpdateResponseAsync("Leveling is already enabled for this server.\nUse `disableleveling` if you wish to disable it.");
            return;
        }
        LevelingData.AddServer(ctx.Guild.Id);
        DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
        eb.WithTitle("Leveling Enabled");
        eb.WithDescription("You have enabled leveling for this server. Here are some tips to help you customize the experience!");
        eb.AddField("Useful Commands", $"`addnoxpchannel`: Blacklists a channel from earning XP. Useful for hidden channels and/or places such as meme chats or bot channels.\n`setlevelupchannel`: Redirects level-up messages to a specific channel. Can be used to keep chat cleaner.\n`addlevelrole`: Adds a role reward for users who reach a certain level.\nSee `{Database.GetOrCreateGuildData(ctx.Guild.Id).GuildPrefix}help leveling` for a full list of commands.");
        await ctx.UpdateResponseAsync(embed: eb.Build());
    }

    [SlashCommand("disable-leveling", "Disables leveling in this server.", false), SlashRequireUserPermissions(Permissions.ManageGuild), LevelingCommand, RequireLeveling]
    public async Task DisableLeveling(InteractionContext ctx, [Option("delete-data", "Whether or not to delete all leveling data for this server")] bool deleteData)
    {
        await ctx.IndicateResponseAsync();
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        LevelingData.RemoveServer(ctx.Guild.Id, deleteData);
        await ctx.UpdateResponseAsync("Successfully disabled leveling.");
    }

    [SlashCommand("add-no-xp-channel", "Blacklists a channel from earning XP.", false), SlashRequireUserPermissions(Permissions.ManageGuild), LevelingCommand, RequireLeveling]
    public async Task AddNoXPChannel(InteractionContext ctx, [Option("channel", "The channel to blacklist.")] DiscordChannel channel)
    {
        await ctx.IndicateResponseAsync();
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.UpdateResponseAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        if (!LevelingData.Servers[ctx.Guild.Id].NoXPChannels.Contains(channel.Id))
            LevelingData.Servers[ctx.Guild.Id].NoXPChannels.Add(channel.Id);
        LevelingData.Servers[ctx.Guild.Id].FlushData();
        await ctx.UpdateResponseAsync($"Added XP-blocked channel: <#{channel.Id}>");
    }

    [SlashCommand("remove-no-xp-channel", "Removes a channel from the XP blacklist.", false), SlashRequireUserPermissions(Permissions.ManageGuild), LevelingCommand, RequireLeveling]
    public async Task RemoveNoXPChannel(InteractionContext ctx, [Option("channel", "The channel to remove.")] DiscordChannel channel)
    {
        await ctx.IndicateResponseAsync();
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.UpdateResponseAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        LevelingData.Servers[ctx.Guild.Id].NoXPChannels.RemoveAll(x => x == channel.Id);
        LevelingData.Servers[ctx.Guild.Id].FlushData();
        await ctx.UpdateResponseAsync($"Removed XP-blocked channel: <#{channel.Id}>");
    }

    [SlashCommand("set-level-up-channel", "Sets a channel to send all level-up messages in.", false), SlashRequireUserPermissions(Permissions.ManageGuild), LevelingCommand, RequireLeveling]
    public async Task SetLevelUpChannel(InteractionContext ctx, [Option("channel", "The channel to set.")] DiscordChannel channel)
    {
        await ctx.IndicateResponseAsync();
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.UpdateResponseAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        LevelingData.Servers[ctx.Guild.Id].SetLevelUpChannel(channel.Id);
        LevelingData.Servers[ctx.Guild.Id].FlushData();
        await ctx.RespondAsync($"Set level-up message channel: <#{channel.Id}>");
    }

    [SlashCommand("remove-level-up-channel", "Removes the channel all level-up messages are being sent to.", false), SlashRequireUserPermissions(Permissions.ManageGuild), LevelingCommand, RequireLeveling]
    public async Task RemoveLevelUpChannel(InteractionContext ctx)
    {
        await ctx.IndicateResponseAsync();
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.UpdateResponseAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        LevelingData.Servers[ctx.Guild.Id].SetLevelUpChannel(null);
        LevelingData.Servers[ctx.Guild.Id].FlushData();
        await ctx.RespondAsync("Removed level-up message channel.");
    }

    [SlashCommand("add-level-up-message", "Adds a message that will be displayed to a user who reaches a certain level.", false), SlashRequireUserPermissions(Permissions.ManageGuild), LevelingCommand, RequireLeveling]
    public async Task AddLevelUpMessage(InteractionContext ctx, [Option("level", "The level to show the message at.")] long levell, [Option("message", "The message to show.")] string message)
    {
        await ctx.IndicateResponseAsync();
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.UpdateResponseAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        if (levell > int.MaxValue || levell < 1)
        {
            await ctx.UpdateResponseAsync("Invalid level!");
            return;
        }
        int level = (int)levell;
        if (!LevelingData.Servers[ctx.Guild.Id].LevelUpMessages.ContainsKey(level))
            LevelingData.Servers[ctx.Guild.Id].LevelUpMessages.Add(level, message);
        else
            LevelingData.Servers[ctx.Guild.Id].LevelUpMessages[level] = message;
        await ctx.UpdateResponseAsync($"Set message for level {level}.");
        LevelingData.Servers[ctx.Guild.Id].FlushData();
    }

    [SlashCommand("remove-level-up-message", "Removes a level-up message from the list to be shown to users who reach a certain level.", false), SlashRequireUserPermissions(Permissions.ManageGuild), LevelingCommand, RequireLeveling]
    public async Task RemoveLevelUpMessage(InteractionContext ctx, [Option("level", "The level to remove the message from.")] long levell)
    {
        await ctx.IndicateResponseAsync();
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.UpdateResponseAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        if (levell > int.MaxValue || levell < 1)
        {
            await ctx.UpdateResponseAsync("Invalid level!");
            return;
        }
        int level = (int)levell;
        if (LevelingData.Servers[ctx.Guild.Id].LevelUpMessages.ContainsKey(level))
        {
            LevelingData.Servers[ctx.Guild.Id].LevelUpMessages.Remove(level);
        }
        await ctx.UpdateResponseAsync($"Removed level-up message for level {level}.");
        LevelingData.Servers[ctx.Guild.Id].FlushData();
    }

    [SlashCommand("clear-level-up-messages", "Clears all level-up messages in this server."), SlashRequireUserPermissions(Permissions.ManageGuild), LevelingCommand, RequireLeveling]
    public async Task ClearLevelUpMessages(InteractionContext ctx, [Option("confirm", "Whether to confirm deletion.")] bool confirm = false)
    {
        await ctx.IndicateResponseAsync();
        if (!confirm)
        {
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
                .WithTitle("Confirm Action")
                .WithColor(Utils.CBOrange)
                .WithDescription("**Warning:** you are about to delete **all** of the level-up messages in this server. Use `clear-level-up-messages True` to continue.");
            await ctx.UpdateResponseAsync(eb.Build());
            return;
        }
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.UpdateResponseAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        LevelingServer server = LevelingData.Servers[ctx.Guild.Id];
        foreach (int level in server.LevelUpMessages.Keys.ToArray())
        {
            server.LevelUpMessages.Remove(level);
        }
        await ctx.UpdateResponseAsync("Removed all level-up messages from this server.");
        server.FlushData();
    }

    [SlashCommand("set-xp-cooldown", "Sets the XP cooldown in this server."), SlashRequireUserPermissions(Permissions.ManageGuild), LevelingCommand, RequireLeveling]
    public async Task SetXPCooldown(InteractionContext ctx, [Option("cooldown", "The cooldown in seconds. Default: 60.")] long cooldownl)
    {
        await ctx.IndicateResponseAsync();
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.UpdateResponseAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        if (cooldownl > int.MaxValue || cooldownl < 1)
        {
            await ctx.UpdateResponseAsync("Invalid cooldown!");
            return;
        }
        int cooldown = (int)cooldownl;
        LevelingData.Servers[ctx.Guild.Id].XPCooldown = cooldown;
        await ctx.UpdateResponseAsync($"Set XP cooldown to **{cooldown}** seconds.");
        LevelingData.Servers[ctx.Guild.Id].FlushData();
    }

    [SlashCommand("set-xp-per-level", "Sets the XP per level in this server."), SlashRequireUserPermissions(Permissions.ManageGuild), LevelingCommand, RequireLeveling]
    public async Task SetXPPerLevel(InteractionContext ctx, [Option("xp", "The amount of XP required to reach level 1. Default: 150.")] long xpl)
    {
        await ctx.IndicateResponseAsync();
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.UpdateResponseAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        if (xpl > int.MaxValue || xpl < 1)
        {
            await ctx.UpdateResponseAsync("Invalid XP count!");
            return;
        }
        int xp = (int)xpl;
        LevelingData.Servers[ctx.Guild.Id].XPPerLevel = xp;
        await ctx.UpdateResponseAsync($"Set XP per level to **{xp}**.");
        LevelingData.Servers[ctx.Guild.Id].FlushData();
    }

    [SlashCommand("set-xp-rate-of-change", "Sets the rate of change of XP required per level in this server."), SlashRequireUserPermissions(Permissions.ManageGuild), LevelingCommand, RequireLeveling]
    public async Task SetXPRateOfChange(InteractionContext ctx, [Option("rate", "The number of times to add the level 1 XP requirement to each new level's requirement. Default: 1.")] long ratel)
    {
        await ctx.IndicateResponseAsync();
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.UpdateResponseAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        if (ratel > int.MaxValue || ratel < 0)
        {
            await ctx.UpdateResponseAsync("Invalid rate!");
            return;
        }
        int rate = (int)ratel;
        LevelingData.Servers[ctx.Guild.Id].XPRateOfChange = rate;
        await ctx.UpdateResponseAsync($"Set XP per level to **{rate}**.");
        LevelingData.Servers[ctx.Guild.Id].FlushData();
    }

    [SlashCommand("set-xp-per-message", "Sets the XP a user receives for sending one message."), SlashRequireUserPermissions(Permissions.ManageGuild), LevelingCommand, RequireLeveling]
    public async Task SetXPPerMessage(InteractionContext ctx, [Option("xp", "The amount of XP to give. Default: 5.")] long xpl, [Option("max-xp", "The maximum amount of XP to give. Default: 5.")] long? maxXPl = null)
    {
        await ctx.IndicateResponseAsync();
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.UpdateResponseAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        if (xpl > int.MaxValue || xpl < 1)
        {
            await ctx.UpdateResponseAsync("Invalid XP count!");
            return;
        }
        int xp = (int)xpl;
        if (maxXPl == null)
        {
            LevelingData.Servers[ctx.Guild.Id].MinXPPerMessage = xp;
            LevelingData.Servers[ctx.Guild.Id].MaxXPPerMessage = xp;
            await ctx.UpdateResponseAsync($"Set XP per message to **{xp}**.");
        }
        else
        {
            if (maxXPl > int.MaxValue || maxXPl < 1)
            {
                await ctx.UpdateResponseAsync("Invalid maximum XP count!");
                return;
            }
            int maxXP = (int)maxXPl;
            if (xp > maxXP)
            {
                await ctx.UpdateResponseAsync("Minimum XP cannot be greater than maximum XP!");
                return;
            }
            LevelingData.Servers[ctx.Guild.Id].MinXPPerMessage = xp;
            LevelingData.Servers[ctx.Guild.Id].MaxXPPerMessage = maxXP;
            await ctx.UpdateResponseAsync($"Set XP per message to a random value from **{xp}** to **{maxXP}**.");
        }
        LevelingData.Servers[ctx.Guild.Id].FlushData();
    }

    [SlashCommand("leveling-settings", "Shows the settings for leveling in this server."), LevelingCommand, RequireLeveling]
    public async Task LevelingSettings(InteractionContext ctx)
    {
        await ctx.IndicateResponseAsync();
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.UpdateResponseAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        LevelingServer lvlServer = LevelingData.Servers[ctx.Guild.Id];
        DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
        eb.WithTitle($"Leveling Settings for {ctx.Guild.Name}");
        if (lvlServer.LevelUpChannel != null)
            eb.AddField("Level-Up Channel", $"<#{lvlServer.LevelUpChannel}>");
        eb.AddField("XP Cooldown", $"{lvlServer.XPCooldown}");
        eb.AddField("XP Per Level", $"{lvlServer.XPPerLevel}", true);
        eb.AddField("XP Rate of Change", $"{lvlServer.XPRateOfChange}", true);
        eb.AddField("Min XP Per Message", $"{lvlServer.MinXPPerMessage}");
        eb.AddField("Max XP Per Message", $"{lvlServer.MaxXPPerMessage}", true);
        eb.AddField("Cumulative Roles", $"{lvlServer.CumulativeRoles}");
        if (lvlServer.NoXPChannels.Count > 0)
        {
            string noXPChannels = "";
            foreach (ulong channel in lvlServer.NoXPChannels)
            {
                noXPChannels += $"\n<#{channel}>";
            }
            noXPChannels = noXPChannels.Trim();
            eb.AddField("XP-Blocked Channels", noXPChannels);
        }
        eb.WithColor(Utils.CBOrange);
        await ctx.UpdateResponseAsync(eb.Build());
    }

    [SlashCommand("reset-levels", "Resets XP and levels."), SlashRequireUserPermissions(Permissions.ManageGuild), LevelingCommand, RequireLeveling]
    public async Task ResetLevels(InteractionContext ctx, [Option("confirm", "Whether or not to confirm the level reset.")] bool confirm = false)
    {
        await ctx.IndicateResponseAsync();
        if (!LevelingData.Servers.ContainsKey(ctx.Guild.Id))
        {
            await ctx.RespondAsync("Leveling is not enabled for this server.\nUse `enable-leveling` if you wish to enable it.");
            return;
        }
        if (confirm)
        {
            try
            {
                var levelingServer = LevelingData.Servers[ctx.Guild.Id];
                levelingServer.Users = new Dictionary<ulong, LevelingUser>();
                levelingServer.UsersByRank = new List<LevelingUser>();
                levelingServer.FlushData();
                await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithTitle("Reset Complete").WithDescription("Successfully reset all levels to 0.").WithColor(Utils.CBOrange));
            }
            catch
            {
                await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithTitle("Reset Failed").WithDescription("Something went wrong. Please try again.").WithColor(DiscordColor.Red));
            }
        }
        else
        {
            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithTitle("Confirm Reset").WithDescription("Are you sure? This action will **irreversibly** reset all levels to 0.\nUse `resetlevels true` to continue.\nIf you instead wish to reset leveling settings, use `resetlevelsettings`.").WithColor(Utils.CBOrange));
        }
    }

    [SlashCommand("reset-level-settings", "Resets leveling settings to their defaults."), SlashRequireUserPermissions(Permissions.ManageGuild), LevelingCommand, RequireLeveling]
    public async Task ResetLevelSettings(InteractionContext ctx, [Option("confirm", "Whether or not to confirm the level settings reset.")] bool confirm = false)
    {
        await ctx.IndicateResponseAsync();
        if (confirm)
        {
            try
            {
                var levelingServer = LevelingData.Servers[ctx.Guild.Id];
                levelingServer.LevelUpChannel = null;
                levelingServer.LevelUpMessages = new Dictionary<int, string>();
                levelingServer.MaxXPPerMessage = 5;
                levelingServer.MinXPPerMessage = 5;
                levelingServer.RoleRewards = new Dictionary<int, ulong>();
                levelingServer.XPCooldown = 60;
                levelingServer.XPPerLevel = 150;
                levelingServer.MentionForLevelUp = false;
                levelingServer.NoXPChannels = new List<ulong>();
                levelingServer.FlushData();
                await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithTitle("Reset Complete").WithDescription("Successfully reset all level settings to the defaults.").WithColor(Utils.CBOrange));
            }
            catch
            {
                await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithTitle("Reset Failed").WithDescription("Something went wrong. Please try again.").WithColor(DiscordColor.Red));
            }
        }
        else
        {
            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithTitle("Confirm Reset").WithDescription("Are you sure? This action will **irreversibly** reset all level settings to the defaults.\nUse `resetlevelsettings true` to continue.\nIf you instead wish to reset levels, use `resetlevels`.").WithColor(Utils.CBOrange));
        }
    }

    [SlashCommand("set-cumulative-roles", "Sets whether level-up roles should be cumulative(combine) or the default(replace)."), LevelingCommand, RequireLeveling, SlashRequireUserPermissions(Permissions.ManageGuild)]
    public async Task SetCumulativeRoles(InteractionContext ctx, [Option("cumulative", "Whether the roles should be cumulative or not.")] bool cumulative)
    {
        await ctx.IndicateResponseAsync();
        LevelingData.Servers[ctx.Guild.Id].CumulativeRoles = cumulative;
        LevelingData.Servers[ctx.Guild.Id].FlushData();
        await ctx.UpdateResponseAsync($"Set cumulative roles to **{cumulative}**.");
    }
}