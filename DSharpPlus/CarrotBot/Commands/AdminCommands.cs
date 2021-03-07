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
    }
}