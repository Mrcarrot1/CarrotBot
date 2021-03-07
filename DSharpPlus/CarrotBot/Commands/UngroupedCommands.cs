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
    public class UngroupedCommands
    {
        /*[Command("help")]
        public async Task Help(CommandContext ctx)
        {
            await ctx.RespondAsync("This version of CarrotBot is very much a work in progress.\nDocumentation is coming soon.");
        }*/
        /*[Command("webhookme")]
        public async Task Webhook(CommandContext ctx, [RemainingText]string message)
        {
            DiscordWebhookBuilder webhookBuilder = new DiscordWebhookBuilder();
            //await hook.ModifyAsync(base64avatar: ctx.Member.AvatarUrl);
            await hook.ExecuteAsync(message);
        }*/
        [Command("say")]
        public async Task Say(CommandContext ctx, string message, ulong channelId = 0)
        {
            if(ctx.User.Id != 366298290377195522) return;
            if(channelId == 0)
                await ctx.RespondAsync(message);
            else
            {
                try
                {
                    DiscordChannel channel = Program.discord.GetChannelAsync(channelId).Result;
                    await channel.SendMessageAsync(message);
                    await ctx.RespondAsync($"Sent message to channel <#{channelId}> (#{channel.Name})");
                }
                catch (Exception e)
                {
                    await ctx.RespondAsync(e.ToString());
                }
            }
        }
        [Command("shutdown")]
        public async Task Shutdown(CommandContext ctx)
        {
            if(ctx.User.Id != 366298290377195522) return;
            await ctx.RespondAsync("CarrotBot shutting down. Good night!");
            Logger.Log("Bot shutting down.");
            Console.WriteLine();
            Environment.Exit(0);
        }
        [Command("restart")]
        public async Task Restart(CommandContext ctx)
        {
            if(ctx.User.Id != 366298290377195522) return;
            await ctx.RespondAsync("CarrotBot restarting. Give me a minute...");
            Logger.Log("Bot restarting.");
            Process.Start($@"{Environment.CurrentDirectory}/CarrotBot");
            Console.WriteLine();
            Environment.Exit(0);
        }
        [Command("updateping")]
        public async Task UpdatePing(CommandContext ctx)
        {
            if(!ctx.Guild.Equals(Program.BotGuild)) return;
            DiscordRole role = ctx.Guild.Roles.FirstOrDefault(x => x.Name == "Updoot Ping");
            if(!ctx.Member.Roles.ToList().Contains(role))
            {
                await ctx.Member.GrantRoleAsync(role, "Given by user request");
                await ctx.RespondAsync("Role granted.");
            }     
            else
            {
                await ctx.Member.RevokeRoleAsync(role, "Revoked by user request");
                await ctx.RespondAsync("Role removed.");
            }
        }
    }
}