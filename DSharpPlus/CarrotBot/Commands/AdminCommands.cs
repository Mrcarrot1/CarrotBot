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
    public class AdminCommands
    {
        [Command("clear"), RequirePermissions(Permissions.ManageMessages), RequireUserPermissions(Permissions.ManageMessages)]
        public async Task Clear(CommandContext ctx, int messages)
        {
            var messagesList = ctx.Channel.GetMessagesAsync(messages + 1).Result;
            foreach(DiscordMessage msg in messagesList)
            {
                await msg.DeleteAsync();
            }
        }
        [Command("kick"), RequirePermissions(Permissions.KickMembers), RequireUserPermissions(Permissions.KickMembers)]
        public async Task Kick(CommandContext ctx, string userMention, [RemainingText]string reason = null)
        {
            ulong UserId = Utils.GetId(userMention);
            DiscordMember user = await ctx.Guild.GetMemberAsync(UserId);
            try
            {
                await user.SendMessageAsync($"You have been kicked from {ctx.Guild.Name} by {ctx.User.Username}.");
                if(reason != null)
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
        [Command("ban"), RequirePermissions(Permissions.BanMembers), RequireUserPermissions(Permissions.BanMembers)]
        public async Task Ban(CommandContext ctx, string userMention, [RemainingText]string reason = null)
        {
            ulong userId = Utils.GetId(userMention);
            DiscordMember user = await ctx.Guild.GetMemberAsync(userId);
            try
            {
                await user.SendMessageAsync($"You have been banned from {ctx.Guild.Name} by {ctx.User.Username}.");
                if(reason != null)
                    await user.SendMessageAsync($"Reason for ban: {reason}");
                else
                    await user.SendMessageAsync("No reason given.");
                await user.BanAsync(reason: reason);
                await ctx.RespondAsync($"Banned {user.Username}.");
            }
            catch
            {
                await ctx.RespondAsync("I can't ban that member. Maybe they have higher permissions than me?");
            }
        }
        [Command("unban"), RequirePermissions(Permissions.BanMembers), RequireUserPermissions(Permissions.BanMembers)]
        public async Task Unban(CommandContext ctx, string userMention, [RemainingText]string reason = null)
        {
            ulong userId = Utils.GetId(userMention);
            DiscordUser user = await Program.discord.GetUserAsync(userId);
            try
            {
                await user.UnbanAsync(ctx.Guild);
                await ctx.RespondAsync($"Unbanned {user.Username}.");
            }
            catch
            {
                await ctx.RespondAsync("I can't unban that user. Maybe I don't have permission?");
            }
        }
        [Command("warn"), RequirePermissions(Permissions.ManageGuild)]
        public async Task Warn(CommandContext ctx, string userMention, [RemainingText]string reason)
        {
            ulong userId = Utils.GetId(userMention);
            DiscordEmbedBuilder eb =  new DiscordEmbedBuilder();
            eb.WithAuthor("Warning Issued");
            if(reason != null)
                eb.WithDescription($"Warned <@!{userId}>: **{reason}**");
            else
                eb.WithDescription($"Warned <@!{userId}>. No reason given.");
            GuildUserData user = Database.GetOrCreateGuildData(ctx.Guild.Id).GetOrCreateUserData(userId);
            user.AddWarning(reason, ctx.User.Id);
            user.FlushData();
            await ctx.RespondAsync(embed: eb.Build());
        }
        [Command("warnings")]
        public async Task Warnings(CommandContext ctx, string userMention)
        {
            ulong userId = Utils.GetId(userMention);
            GuildUserData user = Database.GetOrCreateGuildData(ctx.Guild.Id).GetOrCreateUserData(userId);
            if(user.Warnings.Count == 0)
            {
                await ctx.RespondAsync("That user doesn't have any warnings in this server!");
            }
            else foreach(var warning in user.Warnings)
            {
                DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
                eb.WithAuthor($"{ctx.Guild.GetMemberAsync(userId).Result.Username}'s Warnings");
                eb.AddField($"{warning.Item2.ToString("yyyy-MM-dd HH:mm:ss")}", $"Warned by <@!{warning.Item3}>\nReason: {warning.Item1}");
                await ctx.RespondAsync(embed:eb.Build());
            }
        }
        [Command("addjoinrole"), RequirePermissions(Permissions.ManageRoles)]
        public async Task AddJoinRole(CommandContext ctx, string role)
        {
            try
            {
                ulong Id = Utils.GetId(role);
                Database.GetOrCreateGuildData(ctx.Guild.Id).AddJoinRole(Id);
                await ctx.RespondAsync("Added role to grant on join.");
            }
            catch(FormatException)
            {
                await ctx.RespondAsync("I couldn't find that role. Make sure you're using the role's Id or mention!");
            }
        }
        [Command("removejoinrole"), RequirePermissions(Permissions.ManageRoles)]
        public async Task RemoveJoinRole(CommandContext ctx, string role)
        {
            try
            {
                ulong Id = Utils.GetId(role);
                Database.GetOrCreateGuildData(ctx.Guild.Id).RemoveJoinRole(Id);
                await ctx.RespondAsync("Added role to grant on join.");
            }
            catch(FormatException)
            {
                await ctx.RespondAsync("I couldn't find that role. Make sure you're using the role's Id or mention!");
            }
        }
    }
}