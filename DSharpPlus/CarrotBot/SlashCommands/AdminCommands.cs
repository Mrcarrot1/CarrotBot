using System;
using System.Linq;
using System.Threading.Tasks;
using CarrotBot.Data;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace CarrotBot.SlashCommands;

public class AdminCommands : ApplicationCommandModule
{
    [SlashCommand("clear", "Removes the last n messages.", false), SlashRequirePermissions(Permissions.ManageMessages), SlashCommandPermissions(Permissions.ManageMessages)]
    public async Task Clear(InteractionContext ctx, [Option("messages", "The number of messages to remove.")] long messages)
    {
        await ctx.IndicateResponseAsync(true);
        if (messages > 1000 || messages < 1)
        {
            await ctx.UpdateResponseAsync("Please enter a number between 1 and 1000.");
            return;
        }
        var messagesList = (await ctx.Channel.GetMessagesAsync((int)messages + 1)).Where(x => !x.Pinned).ToList();
        for (int i = 1; i < messagesList.Count; i++)
        {
            DiscordMessage msg = messagesList[i];
            await msg.DeleteAsync();
        }
        await ctx.UpdateResponseAsync($"Cleared {messages} messages.");
    }
    /*[SlashCommand("kick", "Kicks a user from the server."), SlashRequirePermissions(Permissions.KickMembers), SlashRequireGuild]
    public async Task Kick(InteractionContext ctx, [Option("userMention", "The user to kick.")] string userMention, [Option("reason", "The reason for kicking the user.")] string reason = null)
    {
        await ctx.IndicateResponseAsync();
        bool dmFailed = false;
        ulong UserId = Utils.GetId(userMention);
        if (!ctx.Guild.Members.ContainsKey(UserId))
        {
            await ctx.UpdateResponseAsync("Couldn't find that member!");
            return;
        }
        DiscordMember user = await ctx.Guild.GetMemberAsync(UserId);
        if (user.Roles.OrderBy(x => x.Position).First().Position >= ctx.Member.Roles.OrderBy(x => x.Position).First().Position)
        {
            await ctx.UpdateResponseAsync("You don't have permission to kick that user!");
            return;
        }
        try
        {
            try
            {
                await user.SendMessageAsync($"You have been kicked from {ctx.Guild.Name} by {ctx.User.Username}.");
                if (reason != null)
                    await user.SendMessageAsync($"Reason for kick: {reason}");
                else
                    await user.SendMessageAsync("No reason given.");
            }
            catch
            {
                dmFailed = true;
            }
            await user.RemoveAsync(reason);
            await ctx.UpdateResponseAsync(dmFailed ? $"Kicked {user.Username}. I couldn't DM them." : $"Kicked {user.Username}.");
        }
        catch
        {
            await ctx.RespondAsync("I can't kick that member. Maybe they have higher permissions than me?");
        }
    }*/

    [SlashCommand("kick", "Kicks a user from the server.", false), SlashRequirePermissions(Permissions.KickMembers), SlashCommandPermissions(Permissions.KickMembers), SlashRequireGuild]
    public async Task Kick(InteractionContext ctx, [Option("user", "The user to kick.")] DiscordUser user, [Option("reason", "The reason for kicking the user.")] string? reason = null)
    {
        await ctx.IndicateResponseAsync();
        bool dmFailed = false;
        DiscordMember? member = user as DiscordMember;
        if (member is not null && member.Roles.Any() && member.Roles.OrderBy(x => x.Position).First().Position >= ctx.Member.Roles.OrderBy(x => x.Position).First().Position)
        {
            await ctx.UpdateResponseAsync("You don't have permission to kick that user!");
            return;
        }
        try
        {
            try
            {
                await member!.SendMessageAsync($"You have been kicked from {ctx.Guild.Name} by {ctx.User.Username}.");
                if (reason != null)
                    await member.SendMessageAsync($"Reason for kick: {reason}");
                else
                    await member.SendMessageAsync("No reason given.");
            }
            catch
            {
                dmFailed = true;
            }
            await member!.RemoveAsync(reason);
            await ctx.UpdateResponseAsync(dmFailed ? $"Kicked {user.Username}. I couldn't DM them." : $"Kicked {user.Username}.");
        }
        catch
        {
            await ctx.UpdateResponseAsync("I can't kick that member. Maybe they have higher permissions than me?");
        }
    }

    /*[SlashCommand("ban", "Bans a user from the server."), SlashRequirePermissions(Permissions.BanMembers), SlashRequireGuild]
    public async Task Ban(InteractionContext ctx, [Option("userMention", "The user to ban.")] string userMention, [Option("reason", "The reason for banning the user.")] string reason = null)
    {
        await ctx.IndicateResponseAsync();
        ulong userId;
        DiscordMember user = null;

        bool dmFailed = false;

        try
        {
            userId = Utils.GetId(userMention);
        }
        catch (FormatException)
        {
            await ctx.UpdateResponseAsync("I can't find that user. Make sure you're mentioning them or using their ID!");
            return;
        }

        if (!ctx.Guild.Members.ContainsKey(userId))
        {
            await ctx.Guild.BanMemberAsync(userId, 0, reason);
            DiscordUser discordUser = await Program.discord.GetShard(0).GetUserAsync(userId);
            await ctx.UpdateResponseAsync($"Banned {discordUser.Username}#{discordUser.Discriminator}");
        }

        user = await ctx.Guild.GetMemberAsync(userId);

        try
        {
            if (user.Roles.OrderBy(x => x.Position).First().Position >= ctx.Member.Roles.OrderBy(x => x.Position).First().Position)
            {
                await ctx.UpdateResponseAsync("You don't have permission to ban that user!");
                return;
            }
            try
            {
                await user.SendMessageAsync($"You have been banned from {ctx.Guild.Name} by {ctx.User.Username}.");
                if (reason != null)
                    await user.SendMessageAsync($"Reason for ban: {reason}");
                else
                    await user.SendMessageAsync("No reason given.");
            }
            catch
            {
                dmFailed = true;
            }
            await user.BanAsync(reason: reason);
            await ctx.UpdateResponseAsync(dmFailed ? $"Banned {user.Username}. I couldn't DM them." : $"Banned {user.Username}.");
        }
        catch
        {
            await ctx.UpdateResponseAsync("I can't ban that member. Maybe they have higher permissions than me?");
        }
    }*/

    [SlashCommand("ban", "Bans a user from the server."), SlashRequirePermissions(Permissions.BanMembers), SlashCommandPermissions(Permissions.BanMembers), SlashRequireGuild]
    public async Task Ban(InteractionContext ctx, [Option("user", "The user to ban.")] DiscordUser user, [Option("reason", "The reason for banning the user.")] string? reason = null)
    {
        await ctx.IndicateResponseAsync();
        if (!ctx.Guild.Members.ContainsKey(user.Id))
        {
            await ctx.Guild.BanMemberAsync(user.Id, reason: reason);
            await ctx.UpdateResponseAsync($"Banned {user.Username}#{user.Discriminator}.");
            return;
        }
        DiscordMember? member = user as DiscordMember;

        bool dmFailed = false;

        try
        {
            if (member!.Roles.Any() && member.Roles.OrderBy(x => x.Position).First().Position >= ctx.Member.Roles.OrderBy(x => x.Position).First().Position)
            {
                await ctx.UpdateResponseAsync("You don't have permission to ban that user!");
                return;
            }
            try
            {
                await member.SendMessageAsync($"You have been banned from {ctx.Guild.Name}.");
                if (reason != null)
                    await member.SendMessageAsync($"Reason for ban: {reason}");
                else
                    await member.SendMessageAsync("No reason given.");
            }
            catch
            {
                dmFailed = true;
            }
            await member.BanAsync(reason: reason);
            await ctx.UpdateResponseAsync(dmFailed ? $"Banned {user.Username}#{user.Discriminator}. I couldn't DM them." : $"Banned {user.Username}#{user.Discriminator}.");
        }
        catch
        {
            await ctx.UpdateResponseAsync("I can't ban that member. Maybe they have higher permissions than me?");
        }
    }

    [SlashCommand("unban", "Unbans a user from the server.", false), SlashRequirePermissions(Permissions.BanMembers), SlashRequireGuild]
    public async Task Unban(InteractionContext ctx, [Option("user", "The user to unban.")] DiscordUser user, [Option("reason", "The reason for unbanning the user.")] string? reason = null)
    {
        await ctx.IndicateResponseAsync();
        //DiscordUser user = await Program.discord.ShardClients.First().Value.GetUserAsync(userId);
        try
        {
            await user.UnbanAsync(ctx.Guild, reason);
            await ctx.UpdateResponseAsync($"Unbanned {user.Username}#{user.Discriminator}.");
        }
        catch
        {
            await ctx.UpdateResponseAsync("I can't unban that user. Maybe I don't have permission?");
        }
    }
    //[SlashCommand("warn", "Issues a warning to a user in this server."), SlashRequirePermissions(Permissions.ManageGuild)]
    /*public async Task Warn(InteractionContext ctx, [Option("userMention", "The user to warn.")] string userMention, [Option("reason", "The text of the warning.")] string reason)
    {
        await ctx.IndicateResponseAsync();
        ulong userId = Utils.GetId(userMention);
        DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
        eb.WithAuthor("Warning Issued");
        eb.WithColor(Utils.CBOrange);
        if (reason != null)
            eb.WithDescription($"Warned <@!{userId}>: **{reason}**");
        else
            eb.WithDescription($"Warned <@!{userId}>. No reason given.");
        GuildUserData user = Database.GetOrCreateGuildData(ctx.Guild.Id).GetOrCreateUserData(userId);
        if (reason == "" || reason == null)
            reason = "No reason given.";
        user.AddWarning(reason, ctx.User.Id);
        user.FlushData();
        await ctx.UpdateResponseAsync(eb.Build());
    }*/
    [SlashCommand("warn", "Issues a warning to a user in this server.", false), SlashRequirePermissions(Permissions.ManageGuild)]
    public async Task Warn(InteractionContext ctx, [Option("user", "The user to warn.")] DiscordUser user, [Option("reason", "The text of the warning.")] string reason)
    {
        await ctx.IndicateResponseAsync();
        ulong userId = user.Id;
        DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
        eb.WithAuthor("Warning Issued");
        eb.WithColor(Utils.CBOrange);
        eb.WithDescription($"Warned <@!{userId}>: **{reason}**");
        GuildUserData userData = Database.GetOrCreateGuildData(ctx.Guild.Id).GetOrCreateUserData(userId);
        if (string.IsNullOrEmpty(reason))
            reason = "No reason given.";
        userData.AddWarning(reason, ctx.User.Id);
        userData.FlushData();
        await ctx.UpdateResponseAsync(eb.Build());
    }
    /*[SlashCommand("warnings", "Allows a user to check warnings in this server.")]
    public async Task Warnings(InteractionContext ctx, [Option("userMention", "The user to check warnings for. Leave blank to check your own.")] string userMention = null)
    {
        await ctx.IndicateResponseAsync();
        ulong userId = ctx.User.Id;
        if (userMention != null)
            userId = Utils.GetId(userMention);
        GuildUserData user = Database.GetOrCreateGuildData(ctx.Guild.Id).GetOrCreateUserData(userId);
        if (user.Warnings.Count == 0)
        {
            await ctx.UpdateResponseAsync("That user doesn't have any warnings in this server!");
        }
        else foreach (var warning in user.Warnings)
            {
                DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
                eb.WithAuthor($"{(await ctx.Guild.GetMemberAsync(userId)).Username}'s Warnings");
                eb.AddField($"{warning.Item2.ToString("yyyy-MM-dd HH:mm:ss")}", $"Warned by <@!{warning.Item3}>\nReason: {warning.Item1}");
                await ctx.UpdateResponseAsync(eb.Build());
            }
    }*/
    [SlashCommand("warnings", "Allows a user to check warnings in this server.")]
    public async Task Warnings(InteractionContext ctx, [Option("userMention", "The user to check warnings for. Leave blank to check your own.")] DiscordUser? user = null)
    {
        await ctx.IndicateResponseAsync();
        ulong userId = ctx.User.Id;
        if (user is not null)
            userId = user.Id;
        GuildUserData userData = Database.GetOrCreateGuildData(ctx.Guild.Id).GetOrCreateUserData(userId);
        if (userData.Warnings.Count == 0)
        {
            await ctx.UpdateResponseAsync("That user doesn't have any warnings in this server.");
        }
        else
            foreach (var warning in userData.Warnings)
            {
                DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
                eb.WithAuthor($"{(await ctx.Guild.GetMemberAsync(userId)).Username}'s Warnings");
                eb.AddField($"{warning.Item2:yyyy-MM-dd HH:mm:ss}", $"Warned by <@!{warning.Item3}>\nReason: {warning.Item1}");
                await ctx.UpdateResponseAsync(eb.Build());
            }
    }
    [SlashCommand("add-join-role", "Adds a role that will be assigned to members on joining the server.", false), SlashRequirePermissions(Permissions.ManageRoles)]
    public async Task AddJoinRole(InteractionContext ctx, [Option("role", "The role to add.")] DiscordRole role)
    {
        await ctx.IndicateResponseAsync();
        Database.GetOrCreateGuildData(ctx.Guild.Id).AddJoinRole(role.Id);
        await ctx.UpdateResponseAsync("Added role to grant on join.");
    }
    [SlashCommand("remove-join-role", "Removes a role from being assigned to members on joining the server.", false), SlashRequirePermissions(Permissions.ManageRoles)]
    public async Task RemoveJoinRole(InteractionContext ctx, [Option("role", "The role to remove.")] DiscordRole role)
    {
        await ctx.IndicateResponseAsync();
        Database.GetOrCreateGuildData(ctx.Guild.Id).RemoveJoinRole(role.Id);
        await ctx.UpdateResponseAsync("Removed role from being granted on join.");
    }
    [SlashCommand("modmail-setup", "Sets up a channel to receive modmail messages in.", false), SlashRequireUserPermissions(Permissions.ManageGuild), SlashRequireGuild]
    public async Task ModmailSetup(InteractionContext ctx, [Option("channel", "The channel to receive messages in.")] DiscordChannel channel)
    {
        await ctx.IndicateResponseAsync();
        GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
        guildData.ModMailChannel = channel.Id;
        guildData.FlushData();
        await ctx.UpdateResponseAsync($"Modmail configured in <#{channel.Id}>.");
    }
    [SlashCommand("modmail-remove", "Removes the modmail channel for this server.", false), SlashRequireUserPermissions(Permissions.ManageGuild), SlashRequireGuild]
    public async Task ModmailRemove(InteractionContext ctx)
    {
        await ctx.IndicateResponseAsync();
        GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
        guildData.ModMailChannel = null;
        guildData.FlushData();
        await ctx.UpdateResponseAsync("Modmail channel(if any) removed.");
    }
    [SlashCommand("set-message-logs-channel", "Sets a channel to log message changes and deletions in this server.", false), SlashRequireUserPermissions(Permissions.ManageGuild), SlashRequireGuild]
    public async Task SetAttachLogsChannel(InteractionContext ctx, [Option("channel", "The channel to set up.")] DiscordChannel channel)
    {
        await ctx.IndicateResponseAsync();
        GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
        guildData.MessageLogsChannel = channel.Id;
        guildData.FlushData();
        await ctx.UpdateResponseAsync($"Message logs configured in <#{channel.Id}>.");
    }
    [SlashCommand("remove-message-logs-channel", "Removes the configured message log channel(if any).", false), SlashRequireUserPermissions(Permissions.ManageGuild), SlashRequireGuild]
    public async Task RemoveAttachLogsChannel(InteractionContext ctx)
    {
        await ctx.IndicateResponseAsync();
        GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
        guildData.MessageLogsChannel = null;
        guildData.FlushData();
        await ctx.UpdateResponseAsync("Message log channel(if any) removed.");
    }

    [SlashCommand("custom-roles-set", "Sets whether and how the custom roles module will be enabled in this server."), SlashRequirePermissions(Permissions.ManageRoles)]
    public async Task CustomRolesSet(InteractionContext ctx, [Option("choice", "The option to set.")] GuildData.AllowCustomRoles choice)
    {
        await ctx.IndicateResponseAsync();
        GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
        guildData.CustomRolesAllowed = choice;
        guildData.FlushData();
        DiscordEmbedBuilder eb = new()
        {
            Title = "Success",
            Color = Utils.CBGreen,
            Description = $"Set custom roles to **{choice switch
            {
                GuildData.AllowCustomRoles.All => "All Members",
                GuildData.AllowCustomRoles.Booster => "Boosters Only",
                _ => "None",
            }}**."
        };
        await ctx.UpdateResponseAsync(eb.Build());
    }
}
