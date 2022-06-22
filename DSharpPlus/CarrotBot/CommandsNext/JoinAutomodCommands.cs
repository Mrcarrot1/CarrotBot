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
using CarrotBot.Data;

namespace CarrotBot.Commands
{
    [Group("joinfilter"), Description("Commands for working with regex join filters."), Aliases("joinfilters", "regexfilters")]
    public class JoinFilterCommands : BaseCommandModule
    {
        [Command("add"), RequirePermissions(Permissions.BanMembers, false), Description("Adds a regex filter to autoremove members joining the server.")]
        public async Task AddJoinFilter(CommandContext ctx, [Description("Regex filter to disallow.")] string filter, [Description("Whether or not to ban members(as opposed to kicking them).")] bool ban = true)
        {
            try
            {
                if (filter[0] == '`' && filter[filter.Length - 1] == '`')
                {
                    filter = filter.SafeSubstring(1, filter.Length - 2);
                }
                GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
                guildData.JoinFilters.Add(new JoinFilter(filter, ban));
                guildData.FlushData();
                await ctx.RespondAsync("Successfully added regex filter to " + (ban ? "ban" : "kick") + $" all new members matching `{filter}`.");
            }
            catch
            {
                await ctx.RespondAsync("Something went wrong. Make sure your filter is a valid regular expression!");
            }
        }
        [Command("list"), RequirePermissions(Permissions.ManageGuild, false), Description("Lists the regex filters for users on join.")]
        public async Task ListJoinFilters(CommandContext ctx, [Description("The page to show.")] int page = 1)
        {
            GuildData guild = Database.GetOrCreateGuildData(ctx.Guild.Id);

            int startIndex = 0 + 8 * (page - 1);
            int filtersToShow = guild.JoinFilters.Count - startIndex < 8 ? guild.JoinFilters.Count - startIndex : 8;
            if (filtersToShow == 0 && page == 1)
            {
                await ctx.RespondAsync("No filters to show.");
                return;
            }
            if (filtersToShow < 1 || page < 1)
            {
                await ctx.RespondAsync("Invalid page number!");
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
            await ctx.RespondAsync(embed: eb.Build());
        }

        [Command("remove"), Description("Removes a regex join filter.")]
        public async Task RemoveFilter(CommandContext ctx, [Description("The numeric ID of the filter to remove.")] int filterId)
        {
            try
            {
                GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
                string filter = guildData.JoinFilters[filterId].Regex.ToString();
                guildData.JoinFilters.RemoveAt(filterId);
                guildData.FlushData();

                await ctx.RespondEmbedAsync("Success", $"Successfully removed filter {filterId} (`{filter}`).", Utils.CBGreen);
            }
            catch (Exception e)
            {
                if (e is ArgumentOutOfRangeException || e is IndexOutOfRangeException)
                    await ctx.RespondEmbedAsync(null, "Couldn't find a filter with that number!", DiscordColor.Red);
                else
                    throw;
            }
        }

        [Group("modify"), Description("Commands for modifying a regex join filter.")]
        public class JoinFilterModificationCommands : BaseCommandModule
        {
            [Command("ban"), Description("Sets whether the filter should ban users(as opposed to kicking them).")]
            public async Task ModifyBan(CommandContext ctx, [Description("The numeric ID of the filter to modify.")] int filterId, [Description("Whether the filter should ban users.")] bool ban)
            {
                try
                {
                    GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
                    JoinFilter filter = guildData.JoinFilters[filterId];

                    filter.Ban = ban;
                    guildData.FlushData();
                    await ctx.RespondEmbedAsync("Success", $"Successfully set filter {filterId} (`{filter.Regex.ToString()}`) to " + (ban ? "ban" : "kick") + " users.", Utils.CBGreen);
                }
                catch (IndexOutOfRangeException)
                {
                    await ctx.RespondEmbedAsync(null, "Couldn't find a filter with that number!", DiscordColor.Red);
                }
            }

            [Command("regex"), Description("Used to modify the regex used in the filter.")]
            public async Task ModifyRegex(CommandContext ctx, [Description("The numeric ID of the filter to modify.")] int filterId, [Description("The regex the filter should use.")] string regex)
            {
                try
                {
                    GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
                    JoinFilter filter = guildData.JoinFilters[filterId];

                    filter.Regex = new System.Text.RegularExpressions.Regex(regex);
                    guildData.FlushData();
                    await ctx.RespondEmbedAsync("Success", $"Successfully set filter {filterId} to `{filter.ToString()}`.", Utils.CBGreen);
                }
                catch (IndexOutOfRangeException)
                {
                    await ctx.RespondEmbedAsync(null, "Couldn't find a filter with that number!", DiscordColor.Red);
                }
            }
        }
    }

    [Group("joinblacklist"), Description("Commands for working with exact join blacklists."), Aliases("joinblacklists", "exactblacklists", "exactblacklist")]
    public class JoinBlacklistCommands : BaseCommandModule
    {
        [Command("add"), RequirePermissions(Permissions.BanMembers, false), Description("Adds an exact blacklist to autoremove members joining the server.")]
        public async Task AddJoinBlacklist(CommandContext ctx, [Description("The username to disallow.")] string blacklist, [Description("Whether or not to ban members(as opposed to kicking them).")] bool ban = true)
        {
            try
            {
                GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
                guildData.JoinBlacklists.Add(new JoinBlacklist(blacklist, ban));
                guildData.FlushData();
                await ctx.RespondAsync("Successfully added blacklist entry to automatically " + (ban ? "ban" : "kick") + $" all new members with the username **{blacklist}**.");
            }
            catch
            {
                await ctx.RespondAsync("Something went wrong.");
            }
        }
        [Command("list"), RequirePermissions(Permissions.ManageGuild, false), Description("Lists the username blacklists for users on join.")]
        public async Task ListJoinBlacklists(CommandContext ctx, [Description("The page to show.")] int page = 1)
        {
            GuildData guild = Database.GetOrCreateGuildData(ctx.Guild.Id);

            int startIndex = 0 + 8 * (page - 1);
            int blacklistsToShow = guild.JoinBlacklists.Count - startIndex < 8 ? guild.JoinBlacklists.Count - startIndex : 8;
            if (blacklistsToShow == 0 && page == 1)
            {
                await ctx.RespondAsync("No blacklist entries to show.");
                return;
            }
            if (blacklistsToShow < 1 || page < 1)
            {
                await ctx.RespondAsync("Invalid page number!");
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
            await ctx.RespondAsync(embed: eb.Build());

        }

        [Command("remove"), Description("Removes an exact join blacklist.")]
        public async Task RemoveFilter(CommandContext ctx, [Description("The numeric ID of the blacklist to remove.")] int blacklistId)
        {
            try
            {
                GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
                string username = guildData.JoinBlacklists[blacklistId].Username;
                guildData.JoinBlacklists.RemoveAt(blacklistId);
                guildData.FlushData();

                await ctx.RespondEmbedAsync("Success", $"Successfully removed blacklist entry {blacklistId} ({username}).", Utils.CBGreen);
            }
            catch (Exception e)
            {
                if (e is ArgumentOutOfRangeException || e is IndexOutOfRangeException)
                    await ctx.RespondEmbedAsync(null, "Couldn't find a blacklist entry with that number!", DiscordColor.Red);
                else
                    throw;
            }
        }

        [Group("modify"), Description("Commands for modifying an exact join blacklist.")]
        public class JoinFilterModificationCommands : BaseCommandModule
        {
            [Command("ban"), Description("Sets whether the blacklist entry should ban users(as opposed to kicking them).")]
            public async Task ModifyBan(CommandContext ctx, [Description("The numeric ID of the blacklist entry to modify.")] int blacklistId, [Description("Whether the entry should ban users.")] bool ban)
            {
                try
                {
                    GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
                    JoinBlacklist blacklist = guildData.JoinBlacklists[blacklistId];

                    blacklist.Ban = ban;
                    guildData.FlushData();
                    await ctx.RespondEmbedAsync("Success", $"Successfully set blacklist entry {blacklistId} ({blacklist.Username}) to " + (ban ? "ban" : "kick") + " users.", Utils.CBGreen);
                }
                catch (IndexOutOfRangeException)
                {
                    await ctx.RespondEmbedAsync(null, "Couldn't find a blacklist entry with that number!", DiscordColor.Red);
                }
            }

            [Command("username"), Description("Used to modify the username that is banned.")]
            public async Task ModifyRegex(CommandContext ctx, [Description("The numeric ID of the blacklist entry to modify.")] int blacklistId, [Description("The username to blacklist.")] string username)
            {
                try
                {
                    GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
                    JoinBlacklist blacklist = guildData.JoinBlacklists[blacklistId];

                    blacklist.Username = username;
                    guildData.FlushData();
                    await ctx.RespondEmbedAsync("Success", $"Successfully set blacklist entry {blacklistId} to {blacklist.ToString()}.", Utils.CBGreen);
                }
                catch (IndexOutOfRangeException)
                {
                    await ctx.RespondEmbedAsync(null, "Couldn't find a blacklist entry with that number!", DiscordColor.Red);
                }
            }
        }
    }
}