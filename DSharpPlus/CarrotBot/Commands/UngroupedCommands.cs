using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using CarrotBot.Data;

namespace CarrotBot.Commands
{
    public class UngroupedCommands
    {
        /*[Command("help"), Description("Displays command help.")]
        public async Task Help(CommandContext ctx, [Description("Command to provide help for.")] params string[] command)
        {
            
            /*var topLevel = Program.commands.RegisteredCommands.Distinct();
            var helpBuilder = ctx.CommandsNext.HelpFormatter.Create(ctx);

            if (command != null && command.Any())
            {
                Command cmd = null;
                var searchIn = topLevel;
                foreach (var c in command)
                {
                    if (searchIn == null)
                    {
                        cmd = null;
                        break;
                    }

                    cmd = ctx.Config.CaseSensitive
                        ? searchIn.FirstOrDefault(xc => xc.Name == c || (xc.Aliases != null && xc.Aliases.Contains(c)))
                        : searchIn.FirstOrDefault(xc => xc.Name.ToLowerInvariant() == c.ToLowerInvariant() || (xc.Aliases != null && xc.Aliases.Select(xs => xs.ToLowerInvariant()).Contains(c.ToLowerInvariant())));

                    if (cmd == null)
                        break;

                    var failedChecks = await cmd.RunChecksAsync(ctx, true).ConfigureAwait(false);
                    if (failedChecks.Any())
                        throw new ChecksFailedException(cmd, ctx, failedChecks);

                    searchIn = cmd is CommandGroup ? (cmd as CommandGroup).Children : null;
                }

                if (cmd == null)
                    throw new CommandNotFoundException(string.Join(" ", command));

                helpBuilder.WithCommand(cmd);

                if (cmd is CommandGroup group)
                {
                    var commandsToSearch = group.Children.Where(xc => !xc.IsHidden);
                    var eligibleCommands = new List<Command>();
                    foreach (var candidateCommand in commandsToSearch)
                    {
                        if (candidateCommand.ExecutionChecks == null || !candidateCommand.ExecutionChecks.Any())
                        {
                            eligibleCommands.Add(candidateCommand);
                            continue;
                        }

                        var candidateFailedChecks = await candidateCommand.RunChecksAsync(ctx, true).ConfigureAwait(false);
                        if (!candidateFailedChecks.Any())
                            eligibleCommands.Add(candidateCommand);
                    }

                    if (eligibleCommands.Any())
                        helpBuilder.WithSubcommands(eligibleCommands.OrderBy(xc => xc.Name));
                }
            }
            else
            {
                var commandsToSearch = topLevel.Where(xc => !xc.IsHidden);
                var eligibleCommands = new List<Command>();
                foreach (var sc in commandsToSearch)
                {
                    if (sc.ExecutionChecks == null || !sc.ExecutionChecks.Any())
                    {
                        eligibleCommands.Add(sc);
                        continue;
                    }

                    var candidateFailedChecks = await sc.RunChecksAsync(ctx, true).ConfigureAwait(false);
                    if (!candidateFailedChecks.Any())
                        eligibleCommands.Add(sc);
                }

                if (eligibleCommands.Any())
                    helpBuilder.WithSubcommands(eligibleCommands.OrderBy(xc => xc.Name));
            }

            var helpMessage = helpBuilder.Build();

            var builder = new DiscordMessageBuilder().WithContent(helpMessage.Content).WithEmbed(helpMessage.Embed);

            if (!ctx.Config.DmHelp || ctx.Channel is DiscordDmChannel || ctx.Guild == null)
                await ctx.RespondAsync(builder).ConfigureAwait(false);
            else
                await ctx.Member.SendMessageAsync(builder).ConfigureAwait(false);

            } //end comment here
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
        [Command("afk")]
        public async Task AFK(CommandContext ctx, [RemainingText]string message = "AFK")
        {
            GuildUserData userData = Database.GetOrCreateGuildData(ctx.Guild.Id).GetOrCreateUserData(ctx.User.Id);
            userData.SetAFK(message);
            try
            {
                await ctx.Member.ModifyAsync($"[AFK] {ctx.Member.DisplayName}");
            }
            catch { }
            await ctx.RespondAsync($"Set your AFK: {message}");
        }
    }
}