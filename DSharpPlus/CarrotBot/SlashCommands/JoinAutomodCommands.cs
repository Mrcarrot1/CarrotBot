using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using CarrotBot.Data;

namespace CarrotBot.SlashCommands;
[SlashCommandGroup("joinfilter", "SlashCommands for working with regex join filters.")]
public class JoinFilterCommands : ApplicationCommandModule
{
    [SlashCommand("add", "Adds a regex filter to autoremove members joining the server.", false), SlashRequirePermissions(Permissions.BanMembers, false)]
    public async Task AddJoinFilter(InteractionContext ctx, [Option("filter", "Regex filter to disallow.")] string filter, [Option("ban", "Whether or not to ban members(as opposed to kicking them).")] bool ban = true)
    {
        await ctx.IndicateResponseAsync();
        try
        {
            GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
            guildData.JoinFilters.Add(new JoinFilter(filter, ban, ctx.User.Id));
            guildData.FlushData();
            await ctx.UpdateResponseAsync("Successfully added regex filter to " + (ban ? "ban" : "kick") + $" all new members matching `{filter}`.");
        }
        catch
        {
            await ctx.UpdateResponseAsync("Something went wrong. Make sure your filter is a valid regular expression!");
        }
    }
    [SlashCommand("list", "Lists the regex filters for users on join.", false), SlashRequirePermissions(Permissions.BanMembers, false)]
    public async Task ListJoinFilters(InteractionContext ctx, [Option("page", "The page to show.")] long pagel = 1)
    {
        await ctx.IndicateResponseAsync();
        GuildData guild = Database.GetOrCreateGuildData(ctx.Guild.Id);
        if (pagel > int.MaxValue || pagel < 1)
        {
            await ctx.UpdateResponseAsync("Invalid page!");
            return;
        }
        int page = (int)pagel; //I hate not being able to accept 32-bit integer parameters

        int startIndex = 0 + 8 * (page - 1);
        int filtersToShow = guild.JoinFilters.Count - startIndex < 8 ? guild.JoinFilters.Count - startIndex : 8;
        if (filtersToShow == 0 && page == 1)
        {
            await ctx.UpdateResponseAsync("No filters to show.");
            return;
        }
        if (filtersToShow < 1 || page < 1)
        {
            await ctx.UpdateResponseAsync("Invalid page number!");
            return;
        }

        DiscordEmbedBuilder eb = new DiscordEmbedBuilder
        {
            Title = $"{ctx.Guild.Name} Regex Filters",
            Description = $"**Showing {(page - 1) * 8 + 1}-{(page - 1) * 8 + filtersToShow} of {guild.JoinFilters.Count}**"
        };
        for (int i = startIndex; i - startIndex < filtersToShow; i++)
        {
            JoinFilter filter = guild.JoinFilters[i];
            string ban = filter.Ban ? "Yes" : "No";
            eb.AddField("ID", $"`{i}`", true);
            eb.AddField("Regex", $"`{filter.Regex.ToString()}`", true);
            eb.AddField("Ban?", ban, true);
        }
        eb.WithColor(Utils.CBGreen);
        eb.WithFooter("Showing regex filters ・ Use `joinblacklist list` to view exact blacklist");
        await ctx.UpdateResponseAsync(eb.Build());
    }

    [SlashCommand("info", "Displays various information about a regex join filter.", false), SlashRequirePermissions(Permissions.BanMembers)]
    public async Task FilterInfo(InteractionContext ctx, [Option("filter-id", "The numeric ID of the filter to view.")] long filterIdl)
    {
        await ctx.IndicateResponseAsync();
        try
        {
            if (filterIdl > int.MaxValue || filterIdl < 1)
            {
                await ctx.UpdateResponseAsync("Invalid filter ID!");
                return;
            }
            int filterId = (int)filterIdl;
            GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);

            JoinFilter filter = guildData.JoinFilters[filterId];
            DiscordEmbedBuilder eb = new();

            eb.WithTitle($"Filter {filterId} info");
            eb.AddField("Regex", $"`{filter.Regex.ToString()}`", true);
            eb.AddField("Ban?", (filter.Ban ? "Yes" : "No"), true);
            eb.AddField("Created By", filter.CreatorId != 0 ? $"<@{filter.CreatorId}>" : "Unknown", true);
            string exceptions = "";
            foreach (ulong exception in filter.Exceptions)
            {
                exceptions += $"<@{exception}> ";
            }
            if (exceptions != "")
                eb.AddField("Exceptions", exceptions);

            eb.WithColor(Utils.CBGreen);

            await ctx.UpdateResponseAsync(embed: eb.Build());
        }
        catch (Exception e)
        {
            if (e is ArgumentOutOfRangeException || e is IndexOutOfRangeException)
                await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithDescription("Couldn't find a filter with that number!").WithColor(DiscordColor.Red));
            else
                throw;
        }
    }

    [SlashCommand("remove", "Removes a regex join filter.", false), SlashRequirePermissions(Permissions.BanMembers)]
    public async Task RemoveFilter(InteractionContext ctx, [Option("filter-id", "The numeric ID of the filter to remove.")] long filterIdl)
    {
        await ctx.IndicateResponseAsync();
        try
        {
            if (filterIdl > int.MaxValue || filterIdl < 1)
            {
                await ctx.UpdateResponseAsync("Invalid filter ID!");
                return;
            }
            int filterId = (int)filterIdl;
            GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
            string filter = guildData.JoinFilters[filterId].Regex.ToString();
            guildData.JoinFilters.RemoveAt(filterId);
            guildData.FlushData();

            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithTitle("Success").WithDescription($"Successfully removed filter {filterId} (`{filter}`).").WithColor(Utils.CBGreen));
        }
        catch (Exception e)
        {
            if (e is ArgumentOutOfRangeException || e is IndexOutOfRangeException)
                await ctx.RespondEmbedAsync(null, "Couldn't find a filter with that number!", DiscordColor.Red);
            else
                throw;
        }
    }

    //[SlashCommandGroup("modify", "SlashCommands for modifying a regex join filter.", false)]
    //public class JoinFilterModificationSlashCommands : ApplicationCommandModule
    //{
    [SlashCommand("modify-ban", "Sets whether the filter should ban users(as opposed to kicking them).", false), SlashRequirePermissions(Permissions.BanMembers)]
    public async Task ModifyBan(InteractionContext ctx, [Option("filter-id", "The numeric ID of the filter to modify.")] long filterIdl, [Option("ban", "Whether the filter should ban users.")] bool ban)
    {
        await ctx.IndicateResponseAsync();
        try
        {
            if (filterIdl > int.MaxValue || filterIdl < 1)
            {
                await ctx.UpdateResponseAsync("Invalid filter ID!");
                return;
            }
            int filterId = (int)filterIdl;
            GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
            JoinFilter filter = guildData.JoinFilters[filterId];

            filter.Ban = ban;
            guildData.FlushData();
            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithTitle("Success").WithDescription($"Successfully set filter {filterId} (`{filter.Regex.ToString()}`) to " + (ban ? "ban" : "kick") + " users.").WithColor(Utils.CBGreen));
        }
        catch (IndexOutOfRangeException)
        {
            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithDescription("Couldn't find a filter with that number!").WithColor(DiscordColor.Red));
        }
    }

    [SlashCommand("modify-regex", "Used to modify the regex used in the filter.", false), SlashRequirePermissions(Permissions.BanMembers)]
    public async Task ModifyRegex(InteractionContext ctx, [Option("filter-id", "The numeric ID of the filter to modify.")] long filterIdl, [Option("regex", "The regex the filter should use.")] string regex)
    {
        await ctx.IndicateResponseAsync();
        try
        {
            if (filterIdl > int.MaxValue || filterIdl < 1)
            {
                await ctx.UpdateResponseAsync("Invalid filter ID!");
                return;
            }
            int filterId = (int)filterIdl;
            GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
            JoinFilter filter = guildData.JoinFilters[filterId];

            filter.Regex = new System.Text.RegularExpressions.Regex(regex);
            guildData.FlushData();
            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithTitle("Success").WithDescription($"Successfully set filter {filterId} to `{filter.ToString()}`.").WithColor(Utils.CBGreen));
        }
        catch (IndexOutOfRangeException)
        {
            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithDescription("Couldn't find a filter with that number!").WithColor(DiscordColor.Red));
        }
    }

    [SlashCommand("add-exception", "Adds an exception to the filter rule.", false), SlashRequirePermissions(Permissions.BanMembers)]
    public async Task AddException(InteractionContext ctx, [Option("filter-id", "The numeric ID of the filter to modify.")] long filterIdl, [Option("user", "The user to add an exception for.")] DiscordUser user)
    {
        await ctx.IndicateResponseAsync();
        try
        {
            if (filterIdl > int.MaxValue || filterIdl < 1)
            {
                await ctx.UpdateResponseAsync("Invalid filter ID!");
                return;
            }
            int filterId = (int)filterIdl;
            GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
            JoinFilter filter = guildData.JoinFilters[filterId];

            ulong Id = user.Id;
            filter.Exceptions.Add(Id);
            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithTitle("Success").WithDescription($"Successfully added an exception to the join filter for {user.Username}#{user.Discriminator}.").WithColor(Utils.CBGreen));
        }
        catch (IndexOutOfRangeException)
        {
            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithDescription("Couldn't find a filter with that number!").WithColor(DiscordColor.Red));
        }
    }

    [SlashCommand("remove-exception", "Removes an exception from the filter rule.", false), SlashRequirePermissions(Permissions.BanMembers)]
    public async Task RemoveException(InteractionContext ctx, [Option("filter-id", "The numeric ID of the filter to modify.")] long filterIdl, [Option("user", "The user to remove the exception for.")] DiscordUser user)
    {
        await ctx.IndicateResponseAsync();
        try
        {
            if (filterIdl > int.MaxValue || filterIdl < 1)
            {
                await ctx.UpdateResponseAsync("Invalid filter ID!");
                return;
            }
            int filterId = (int)filterIdl;
            GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
            JoinFilter filter = guildData.JoinFilters[filterId];

            ulong Id = user.Id;
            if (filter.Exceptions.Contains(Id))
            {
                filter.Exceptions.RemoveAll(x => x == Id);
                await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithTitle("Success").WithDescription($"Successfully removed the exception to the join filter for {user.Username}#{user.Discriminator}.").WithColor(Utils.CBGreen));
            }
            else
            {
                await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithDescription($"Couldn't find an exception for {user.Username}#{user.Discriminator}.").WithColor(DiscordColor.Red));
            }
        }
        catch (IndexOutOfRangeException)
        {
            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithDescription("Couldn't find a filter with that number!").WithColor(DiscordColor.Red));
        }
    }
    //}
}

[SlashCommandGroup("joinblacklist", "Commands for working with exact join blacklists.", false)]
public class JoinBlacklistCommands : ApplicationCommandModule
{
    [SlashCommand("add", "Adds an exact blacklist to autoremove members joining the server.", false), SlashRequirePermissions(Permissions.BanMembers, false)]
    public async Task AddJoinBlacklist(InteractionContext ctx, [Option("username", "The username to disallow.")] string blacklist, [Option("ban", "Whether or not to ban members(as opposed to kicking them).")] bool ban = true)
    {
        await ctx.IndicateResponseAsync();
        try
        {
            GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
            guildData.JoinBlacklists.Add(new JoinBlacklist(blacklist, ban, ctx.User.Id));
            guildData.FlushData();
            await ctx.UpdateResponseAsync("Successfully added blacklist entry to automatically " + (ban ? "ban" : "kick") + $" all new members with the username **{blacklist}**.");
        }
        catch
        {
            await ctx.UpdateResponseAsync("Something went wrong.");
        }
    }
    [SlashCommand("list", "Lists the username blacklists for users on join.", false), SlashRequirePermissions(Permissions.BanMembers, false)]
    public async Task ListJoinBlacklists(InteractionContext ctx, [Option("page", "The page to show.")] long pagel = 1)
    {
        await ctx.IndicateResponseAsync();
        GuildData guild = Database.GetOrCreateGuildData(ctx.Guild.Id);

        if (pagel > int.MaxValue || pagel < 1)
        {
            await ctx.UpdateResponseAsync("Invalid page!");
            return;
        }
        int page = (int)pagel;
        int startIndex = 0 + 8 * (page - 1);
        int blacklistsToShow = guild.JoinBlacklists.Count - startIndex < 8 ? guild.JoinBlacklists.Count - startIndex : 8;
        if (blacklistsToShow == 0 && page == 1)
        {
            await ctx.UpdateResponseAsync("No blacklist entries to show.");
            return;
        }
        if (blacklistsToShow < 1 || page < 1)
        {
            await ctx.UpdateResponseAsync("Invalid page number!");
            return;
        }

        DiscordEmbedBuilder eb = new DiscordEmbedBuilder
        {
            Title = $"{ctx.Guild.Name} Username Blacklists",
            Description = $"**Showing {(page - 1) * 8 + 1}-{(page - 1) * 8 + blacklistsToShow} of {guild.JoinBlacklists.Count}\nId\tFilter\t\tBan?**"
        };
        for (int i = startIndex; i < startIndex + blacklistsToShow; i++)
        {
            JoinBlacklist blacklist = guild.JoinBlacklists[i];
            string ban = blacklist.Ban ? "Yes" : "No";
            eb.AddField("ID", $"`{i}`", true);
            eb.AddField("Username", $"{blacklist.Username}", true);
            eb.AddField("Ban?", ban, true);
        }
        eb.WithColor(Utils.CBGreen);
        eb.WithFooter("Showing exact blacklist ・ Use `joinfilters list` to view regex filters");
        await ctx.UpdateResponseAsync(embed: eb.Build());

    }

    [SlashCommand("info", "Displays various information about a join blacklist entry.", false), SlashRequirePermissions(Permissions.BanMembers)]
    public async Task BlacklistInfo(InteractionContext ctx, [Option("blacklist-ID", "The numeric ID of the blacklist entry to view.")] long filterIdl)
    {
        await ctx.IndicateResponseAsync();
        try
        {
            if (filterIdl > int.MaxValue || filterIdl < 1)
            {
                await ctx.UpdateResponseAsync("Invalid blacklist ID!");
                return;
            }
            int filterId = (int)filterIdl;
            GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);

            JoinBlacklist blacklist = guildData.JoinBlacklists[filterId];
            DiscordEmbedBuilder eb = new();

            eb.WithTitle($"Filter {filterId} info");
            eb.AddField("Username", $"{blacklist.Username}", true);
            eb.AddField("Ban?", (blacklist.Ban ? "Yes" : "No"), true);
            eb.AddField("Created By", blacklist.CreatorId != 0 ? $"<@{blacklist.CreatorId}>" : "Unknown", true);
            string exceptions = "";
            foreach (ulong exception in blacklist.Exceptions)
            {
                exceptions += $"<@{exception}> ";
            }
            if (exceptions != "")
                eb.AddField("Exceptions", exceptions);

            eb.WithColor(Utils.CBGreen);

            await ctx.UpdateResponseAsync(embed: eb.Build());
        }
        catch (Exception e)
        {
            if (e is ArgumentOutOfRangeException || e is IndexOutOfRangeException)
                await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithDescription("Couldn't find a filter with that number!").WithColor(DiscordColor.Red));
            else
                throw;
        }
    }

    [SlashCommand("remove", "Removes an exact join blacklist.", false), SlashRequirePermissions(Permissions.BanMembers)]
    public async Task RemoveFilter(InteractionContext ctx, [Option("blacklist-ID", "The numeric ID of the blacklist to remove.")] long blacklistIdl)
    {
        await ctx.IndicateResponseAsync();
        try
        {
            if (blacklistIdl > int.MaxValue || blacklistIdl < 1)
            {
                await ctx.UpdateResponseAsync("Invalid blacklist ID!");
                return;
            }
            int blacklistId = (int)blacklistIdl;
            GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
            string username = guildData.JoinBlacklists[blacklistId].Username;
            guildData.JoinBlacklists.RemoveAt(blacklistId);
            guildData.FlushData();

            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithTitle("Success").WithDescription($"Successfully removed blacklist entry {blacklistId} ({username}).").WithColor(Utils.CBGreen));
        }
        catch (Exception e)
        {
            if (e is ArgumentOutOfRangeException || e is IndexOutOfRangeException)
                await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithDescription("Couldn't find a blacklist entry with that number!").WithColor(DiscordColor.Red));
            else
                throw;
        }
    }

    //[SlashCommandGroup("modify", "SlashCommands for modifying an exact join blacklist.", false)]
    //public class JoinFilterModificationSlashCommands : ApplicationCommandModule
    //{
    [SlashCommand("modify-ban", "Sets whether the blacklist entry should ban users(as opposed to kicking them).", false), SlashRequirePermissions(Permissions.BanMembers)]
    public async Task ModifyBan(InteractionContext ctx, [Option("blacklistID", "The numeric ID of the blacklist entry to modify.")] long blacklistIdl, [Option("ban", "Whether the entry should ban users.")] bool ban)
    {
        await ctx.IndicateResponseAsync();
        try
        {
            if (blacklistIdl > int.MaxValue || blacklistIdl < 1)
            {
                await ctx.UpdateResponseAsync("Invalid blacklist ID!");
                return;
            }
            int blacklistId = (int)blacklistIdl;
            GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
            JoinBlacklist blacklist = guildData.JoinBlacklists[blacklistId];

            blacklist.Ban = ban;
            guildData.FlushData();
            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithTitle("Success").WithDescription($"Successfully set blacklist entry {blacklistId} ({blacklist.Username}) to " + (ban ? "ban" : "kick") + " users.").WithColor(Utils.CBGreen));
        }
        catch (IndexOutOfRangeException)
        {
            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithDescription("Couldn't find a blacklist entry with that number!").WithColor(DiscordColor.Red));
        }
    }

    [SlashCommand("modify-username", "Used to modify the username that is banned.", false), SlashRequirePermissions(Permissions.BanMembers)]
    public async Task ModifyRegex(InteractionContext ctx, [Option("blacklistID", "The numeric ID of the blacklist entry to modify.")] long blacklistIdl, [Option("username", "The username to blacklist.")] string username)
    {
        await ctx.IndicateResponseAsync();
        try
        {
            if (blacklistIdl > int.MaxValue || blacklistIdl < 1)
            {
                await ctx.UpdateResponseAsync("Invalid blacklist ID!");
                return;
            }
            int blacklistId = (int)blacklistIdl;
            GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
            JoinBlacklist blacklist = guildData.JoinBlacklists[blacklistId];

            blacklist.Username = username;
            guildData.FlushData();
            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithTitle("Success").WithDescription($"Successfully set blacklist entry {blacklistId} to {blacklist.ToString()}.").WithColor(Utils.CBGreen));
        }
        catch (IndexOutOfRangeException)
        {
            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithDescription("Couldn't find a blacklist entry with that number!").WithColor(DiscordColor.Red));
        }
    }

    [SlashCommand("add-exception", "Adds an exception to the blacklist rule.", false), SlashRequirePermissions(Permissions.BanMembers)]
    public async Task AddException(InteractionContext ctx, [Option("blacklistID", "The numeric ID of the blacklist entry to modify.")] long blacklistIdl, [Option("user", "The user ID or @-mention to add an exception for.")] DiscordUser user)
    {
        await ctx.IndicateResponseAsync();
        try
        {
            if (blacklistIdl > int.MaxValue || blacklistIdl < 1)
            {
                await ctx.UpdateResponseAsync("Invalid blacklist ID!");
                return;
            }
            int blacklistId = (int)blacklistIdl;
            GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
            JoinBlacklist blacklist = guildData.JoinBlacklists[blacklistId];

            ulong Id = user.Id;
            blacklist.Exceptions.Add(Id);
            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithTitle("Success").WithDescription($"Successfully added an exception to the join blacklist entry for {user.Username}#{user.Discriminator}.").WithColor(Utils.CBGreen));
        }
        catch (IndexOutOfRangeException)
        {
            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithDescription("Couldn't find a blacklist entry with that number!").WithColor(DiscordColor.Red));
        }
    }

    [SlashCommand("remove-exception", "Used to add an exception to the blacklist rule.", false), SlashRequirePermissions(Permissions.BanMembers)]
    public async Task RemoveException(InteractionContext ctx, [Option("blacklistID", "The numeric ID of the blacklist entry to modify.")] long filterIdl, [Option("user", "The user to remove the exception for.")] DiscordUser user)
    {
        try
        {
            if (filterIdl > int.MaxValue || filterIdl < 1)
            {
                await ctx.UpdateResponseAsync("Invalid blacklist ID!");
                return;
            }
            int filterId = (int)filterIdl;
            GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
            JoinBlacklist blacklist = guildData.JoinBlacklists[filterId];

            ulong Id = user.Id;
            if (blacklist.Exceptions.Contains(Id))
            {
                blacklist.Exceptions.RemoveAll(x => x == Id);
                await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithTitle("Success").WithDescription($"Successfully removed the exception to the join blacklist entry for {user.Username}#{user.Discriminator}.").WithColor(Utils.CBGreen));
            }
            else
            {
                await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithDescription($"Couldn't find an exception for {user.Username}#{user.Discriminator}.").WithColor(DiscordColor.Red));
            }
        }
        catch (IndexOutOfRangeException)
        {
            await ctx.UpdateResponseAsync(new DiscordEmbedBuilder().WithDescription("Couldn't find a blacklist entry with that number!").WithColor(DiscordColor.Red));
        }
    }
    //}
}