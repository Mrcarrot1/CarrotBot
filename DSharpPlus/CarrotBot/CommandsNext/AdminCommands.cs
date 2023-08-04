using System;
using System.Linq;
using System.Threading.Tasks;
using CarrotBot.Data;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace CarrotBot.CommandsNext
{
    public class AdminCommands : BaseCommandModule
    {
        [Command("clear"), RequirePermissions(Permissions.ManageMessages), RequireUserPermissions(Permissions.ManageMessages), Description("Removes the last *n* messages.")]
        public async Task Clear(CommandContext ctx, [Description("The number of messages to remove.")] int messages)
        {
            if (messages > 1000 || messages < 1)
            {
                await ctx.RespondAsync("Please enter a number between 1 and 1000.");
                return;
            }
            var messagesList = await ctx.Channel.GetMessagesAsync(messages + 1);
            foreach (DiscordMessage msg in messagesList.Where(x => !x.Pinned))
            {
                await msg.DeleteAsync();
            }
        }
        [Command("kick"), RequirePermissions(Permissions.KickMembers), RequireUserPermissions(Permissions.KickMembers), Description("Kicks a user from the server."), RequireGuild]
        public async Task Kick(CommandContext ctx, [Description("The user to kick.")] string? userMention, [RemainingText, Description("The reason for kicking the user.")] string? reason = null)
        {
            ulong UserId = Utils.GetId(userMention);
            DiscordMember user = await ctx.Guild.GetMemberAsync(UserId);
            if (user.Roles.OrderBy(x => x.Position).First().Position >= ctx.Member?.Roles.OrderBy(x => x.Position).First().Position)
            {
                await ctx.RespondAsync("You don't have permission to kick that user!");
                return;
            }
            try
            {
                await user.SendMessageAsync($"You have been kicked from {ctx.Guild.Name} by {ctx.User.Username}.");
                if (reason != null)
                    await user.SendMessageAsync($"Reason for kick: {reason}");
                else
                    await user.SendMessageAsync("No reason given.");
                await user.RemoveAsync(reason);
                await ctx.RespondAsync($"Kicked {user.Username}.");
            }
            catch
            {
                await ctx.RespondAsync("I can't kick that member. Maybe they have higher permissions than me?");
            }
        }
        [Command("ban"), RequirePermissions(Permissions.BanMembers), RequireUserPermissions(Permissions.BanMembers), Description("Bans a user from the server.")]
        public async Task Ban(CommandContext ctx, [Description("The user to ban.")] string? userMention, [RemainingText, Description("The reason for banning the user.")] string? reason = null)
        {
            DiscordMember? user = null;
            try
            {
                var userId = Utils.GetId(userMention);
                user = await ctx.Guild.GetMemberAsync(userId);
            }
            catch (FormatException)
            {
                await ctx.RespondAsync("I can't find that user. Make sure you're mentioning them or using their ID!");
                return;
            }
            catch
            {
                await ctx.RespondAsync("I can't find that user. Make sure you have the right ID and the user is in this server.");
            }
            try
            {
                if (user?.Roles.OrderBy(x => x.Position).First().Position >= ctx.Member?.Roles.OrderBy(x => x.Position).First().Position)
                {
                    await ctx.RespondAsync("You don't have permission to ban that user!");
                    return;
                }

                if ((await user!.TrySendMessageAsync(
                        $"You have been banned from {ctx.Guild.Name} by {ctx.User.Username}.")) is null)
                    await ctx.RespondAsync("Failed to DM the user.");
                if (reason != null)
                    await user!.TrySendMessageAsync($"Reason for ban: {reason}");
                else
                    await user!.TrySendMessageAsync("No reason given.");
                await user!.BanAsync(reason: reason);
                await ctx.RespondAsync($"Banned {user.Username}.");
            }
            catch
            {
                await ctx.RespondAsync("I can't ban that member. Maybe they have higher permissions than me?");
            }
        }
        [Command("unban"), RequirePermissions(Permissions.BanMembers), RequireUserPermissions(Permissions.BanMembers), Description("Unbans a user from the server.")]
        public async Task Unban(CommandContext ctx, [Description("The user to unban.")] string? userMention, [RemainingText, Description("The reason for unbanning the user.")] string? reason = null)
        {
            ulong userId = Utils.GetId(userMention);
            DiscordUser user = await Program.discord!.ShardClients.First().Value.GetUserAsync(userId);
            try
            {
                await user.UnbanAsync(ctx.Guild, reason);
                await ctx.RespondAsync($"Unbanned {user.Username}.");
            }
            catch
            {
                await ctx.RespondAsync("I can't unban that user. Maybe I don't have permission?");
            }
        }
        [Command("warn"), RequirePermissions(Permissions.ManageGuild), Description("Issues a warning to a user in this server.")]
        public async Task Warn(CommandContext ctx, [Description("The user to warn.")] string? userMention, [RemainingText] string? reason = null)
        {
            ulong userId = Utils.GetId(userMention);
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithAuthor("Warning Issued");
            eb.WithDescription(reason != null
                ? $"Warned <@!{userId}>: **{reason}**"
                : $"Warned <@!{userId}>. No reason given.");
            GuildUserData user = Database.GetOrCreateGuildData(ctx.Guild.Id).GetOrCreateUserData(userId);
            if (string.IsNullOrEmpty(reason))
                reason = "No reason given.";
            user.AddWarning(reason, ctx.User.Id);
            user.FlushData();
            await ctx.RespondAsync(embed: eb.Build());
        }
        [Command("warnings"), Description("Allows a user to check warnings in this server.")]
        public async Task Warnings(CommandContext ctx, [Description("The user to check warnings for. Leave blank to check your own.")] string? userMention = null)
        {
            ulong userId = ctx.User.Id;
            if (userMention != null)
                userId = Utils.GetId(userMention);
            GuildUserData user = Database.GetOrCreateGuildData(ctx.Guild.Id).GetOrCreateUserData(userId);
            if (user.Warnings.Count == 0)
            {
                await ctx.RespondAsync("That user doesn't have any warnings in this server!");
            }
            else
                foreach (var warning in user.Warnings)
                {
                    DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
                    eb.WithAuthor($"{(await ctx.Guild.GetMemberAsync(userId)).Username}'s Warnings");
                    eb.AddField($"{warning.Item2:yyyy-MM-dd HH:mm:ss}", $"Warned by <@!{warning.Item3}>\nReason: {warning.Item1}");
                    await ctx.RespondAsync(embed: eb.Build());
                }
        }
        [Command("addjoinrole"), RequirePermissions(Permissions.ManageRoles), Description("Adds a role that will be assigned to members on joining the server.")]
        public async Task AddJoinRole(CommandContext ctx, [Description("The role to add.")] string? role)
        {
            try
            {
                ulong Id = Utils.GetId(role);
                Database.GetOrCreateGuildData(ctx.Guild.Id).AddJoinRole(Id);
                await ctx.RespondAsync("Added role to grant on join.");
            }
            catch (FormatException)
            {
                await ctx.RespondAsync("I couldn't find that role. Make sure you're using the role's Id or mention!");
            }
        }
        [Command("removejoinrole"), RequirePermissions(Permissions.ManageRoles), Description("Removes a role from being assigned to members on joining the server.")]
        public async Task RemoveJoinRole(CommandContext ctx, [Description("The role to remove.")] string? role)
        {
            try
            {
                ulong Id = Utils.GetId(role);
                Database.GetOrCreateGuildData(ctx.Guild.Id).RemoveJoinRole(Id);
                await ctx.RespondAsync("Removed role from being granted on join.");
            }
            catch (FormatException)
            {
                await ctx.RespondAsync("I couldn't find that role. Make sure you're using the role's Id or mention!");
            }
        }
    }
}