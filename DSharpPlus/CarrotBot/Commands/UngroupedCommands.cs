using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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
    public class UngroupedCommands : BaseCommandModule
    {
        [Command("help"), Description("Displays command help.")]
        public async Task Help(CommandContext ctx, [Description("Command to provide help for.")] params string[] command)
        {
            try {
            var topLevel = ctx.CommandsNext.RegisteredCommands.Distinct();
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle("Help");
            eb.WithColor(Utils.CBGreen);


            if(command != null && command.Any())
            {
                Command cmd = null;
                var searchIn = new List<Command>();
                foreach(var c in topLevel)
                {
                    searchIn.Add(c.Value);
                }
                foreach(var c in command)
                {
                    if(searchIn == null)
                    {
                        cmd = null;
                        break;
                    }

                    cmd = searchIn.FirstOrDefault(xc => xc.Name.ToLowerInvariant() == c.ToLowerInvariant() || (xc.Aliases != null && xc.Aliases.Select(xs => xs.ToLowerInvariant()).Contains(c.ToLowerInvariant())));

                    if (cmd == null)
                        break;

                    var failedChecks = await cmd.RunChecksAsync(ctx, true).ConfigureAwait(false);
                    if (failedChecks.Any())
                        throw new ChecksFailedException(cmd, ctx, failedChecks);

                    searchIn = cmd is CommandGroup ? (cmd as CommandGroup).Children.ToList() : null;
                }
                if (cmd is CommandGroup group)
                {
                    eb.WithDescription($"`{cmd.QualifiedName}`: {cmd.Description}");
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
                    {
                        eligibleCommands = eligibleCommands.OrderBy(x => x.Name).ToList();
                        string subcommands = "None";
                        foreach(Command c in eligibleCommands)
                        {
                            if(subcommands == "None")
                                subcommands = $"`{c.QualifiedName}`";
                            else
                                subcommands += $", `{c.QualifiedName}`";
                        }
                        eb.AddField("Subcommands", subcommands);
                    }
                }
                else
                {
                    eb.WithDescription($"`{cmd.QualifiedName}`: {cmd.Description}");
                    if(cmd.Description == null)
                    {
                        eb.WithDescription($"`{cmd.QualifiedName}`");
                    }
                    if(cmd.Overloads.Last().Arguments.Any())
                    {
                        string Overloads = "";
                        foreach(var arg in cmd.Overloads.Last().Arguments)
                        {
                            string argstr = "";
                            if(arg.IsOptional)
                            {
                                argstr = $"`[{arg.Name}]: {arg.Type}`: {arg.Description} Default Value: {arg.DefaultValue}";
                                if(arg.DefaultValue == null)
                                    argstr = argstr.Replace("Default Value: ", "Default Value: Empty");
                            }
                            else argstr = $"`<{arg.Name}>: {arg.Type}`: {arg.Description}";
                            if(arg.IsCatchAll)
                            {
                                argstr = argstr
                                .Replace($"{arg.Name}>", $"{arg.Name}...>")
                                .Replace($"{arg.Name}]", $"{arg.Name}...]");
                            }
                            if(arg.Description == null)
                            {
                                argstr = argstr
                                .Replace("`: ", "`");
                            }
                            Overloads += $"\n{argstr}";
                        }
                        eb.AddField("Arguments", Overloads.Trim());
                    }
                }
            }
            //List all commands if no input
            else
            {
                eb.WithDescription($"Listing top-level commands and groups. Use `{Program.commandPrefix}help <command/group>` to see subcommands or usage details.");
                if(Database.Guilds[ctx.Guild.Id].GuildPrefix != Program.commandPrefix)
                    eb.WithDescription($"Listing top-level commands and groups. Use `{Database.Guilds[ctx.Guild.Id].GuildPrefix}help <command/group>` to see subcommands or usage details.\nThis server's prefix is `{Database.Guilds[ctx.Guild.Id].GuildPrefix}`. You can also use the prefix `cb%` or <@!{Program.discord.CurrentUser.Id}>.");
                string topLevelCommands = "None";
                string commandGroups = "None";
                foreach(KeyValuePair<string, Command> command1 in topLevel.OrderBy(x => x.Value.Name))
                {
                    Command cmd = command1.Value;

                    if(!ctx.Channel.IsPrivate)
                    {
                        if((cmd.Name == "rank" || cmd.Name == "leaderboard" || cmd.Name == "disableleveling") && !Leveling.LevelingData.Servers.ContainsKey(ctx.Guild.Id)) continue;
                        if(cmd.Name == "enableleveling" && Leveling.LevelingData.Servers.ContainsKey(ctx.Guild.Id)) continue;
                        if(cmd.IsHidden && !(cmd.Name == "dripcoin" && ctx.Guild.Id == 824824193001979924))
                        {
                            continue;
                        }
                    }
                    
                    if(cmd is CommandGroup group)
                    {
                        if(commandGroups.Contains($"`{cmd.Name}`")) continue;
                        if(commandGroups == "None")
                            commandGroups = $"`{cmd.Name}`";
                        else if(!commandGroups.Contains($"`{cmd.Name}`"))
                            commandGroups += $", `{cmd.Name}`";
                    }
                    else
                    {
                        if(topLevelCommands == "None")
                            topLevelCommands = $"`{cmd.Name}`";
                        else if(!topLevelCommands.Contains($"`{cmd.Name}`"))
                            topLevelCommands += $", `{cmd.Name}`";
                    }
                }
                eb.AddField("Commands", topLevelCommands);
                eb.AddField("Groups", commandGroups);
            }
            

            await ctx.RespondAsync(embed: eb.Build());
            }
            catch(Exception e)
            {
                Logger.Log(e.ToString(), Logger.LogLevel.EXC);
            }
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
                    var commandsToSearch = group.Children.Where(xc => !xc.IsHidden);if (cmd is CommandGroup group)
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

            var builder = new DiscordMessageBuilder().WithContent(helpMessage.Conhttp://www.example.com/recepticle.aspxent).WithEmbed(helpMessage.Embed);

            if (!ctx.Config.DmHelp || ctx.Channel is DiscordDmChannel || ctx.Guild == null)
                await ctx.RespondAsync(builder).ConfigureAwait(false);
            else
                await ctx.Member.SendMessageAsync(builder).ConfigureAwailocalDataPathlocalDataPath*/
        }
        /*[Command("webhookme")]
        public async Task Webhook(CommandContext ctx, [RemainingText]string message)
        {
            DiscordWebhookBuilder webhookBuilder = new DiscordWebhookBuilder();
            //await hook.ModifyAsync(base64avatar: ctx.Member.AvatarUrl);
            await hook.ExecuteAsync(message); : BaseCommandModule
        }*/
        [Command("say"), Hidden, RequireOwner]
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
        [Command("shutdown"), Hidden, RequireOwner]
        public async Task Shutdown(CommandContext ctx)
        {
            if(ctx.User.Id != 366298290377195522 && ctx.User.Id != 374283134243700747 && ctx.User.Id != 129329809741447168 && ctx.User.Id != 245703456382386177) return;
            if(ctx.User.Id != 366298290377195522)
            {
                File.WriteAllText($@"{Utils.localDataPath}/DO_NOT_START.cb", "DO_NOT_START");
            }
            await ctx.RespondAsync("CarrotBot shutting down. Good night!");
            Logger.Log($"Bot shutdown initiated by {ctx.User.Username}#{ctx.User.Discriminator}.");
            Console.WriteLine();
            Environment.Exit(0);
        }
        [Command("restart"), Hidden, RequireOwner]
        public async Task Restart(CommandContext ctx)
        {
            if(ctx.User.Id != 366298290377195522) return;
            await ctx.RespondAsync("CarrotBot restarting. Give me a minute...");
            Logger.Log("Bot restarting.");
            Process.Start($@"{Environment.CurrentDirectory}/CarrotBot");
            Console.WriteLine();
            Environment.Exit(0);
        }
        [Command("updateping"), Hidden]
        public async Task UpdatePing(CommandContext ctx)
        {
            if(!ctx.Guild.Equals(Program.BotGuild)) return;
            DiscordRole role = ctx.Guild.Roles.FirstOrDefault(x => x.Value.Name == "Updoot Ping").Value;
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
        [Command("afk"), Description("Sets your AFK message.")]
        public async Task AFK(CommandContext ctx, [RemainingText, Description("The message to set.")]string message = "AFK")
        {
            GuildUserData userData = Database.GetOrCreateGuildData(ctx.Guild.Id).GetOrCreateUserData(ctx.User.Id);
            userData.SetAFK(message);
            try
            {
                await ctx.Member.ModifyAsync(x => x.Nickname = $"[AFK] {ctx.Member.DisplayName}");
            }
            catch { }
            await ctx.RespondAsync($"Set your AFK: {message}");
        }
        private static readonly HttpClient client = new HttpClient();
        [Command("catpic"), Description("Provides a random cat picture courtesy of thecatapi.com")]
        public async Task CatPic(CommandContext ctx)
        {
            try 
            {
                client.DefaultRequestHeaders.Add("x-api-key", SensitiveInformation.catAPIKey);
                var responseString = await client.GetStringAsync("https://api.thecatapi.com/v1/images/search");
                Console.WriteLine("Response string: {0}", responseString);
                string[] splitResponseString = responseString.Split("\"url\":\"");
                string url = "";
                foreach(char c in splitResponseString[1])
                {
                    if(c != '"')
                    {
                        url += c;
                    }
                    else break;
                }
                await ctx.RespondAsync(url);
            }
            catch(Exception e)
            {
                Logger.Log(e.ToString(), Logger.LogLevel.EXC);
            }
        }
        [Command("about"), Description("Shows various information about the bot.")]
        public async Task About(CommandContext ctx)
        {
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle("About CarrotBot");
            eb.WithColor(Utils.CBGreen);
            eb.WithDescription($"CarrotBot is a multipurpose Discord bot made by Mrcarrot#3305. Use `{Program.commandPrefix}help` for command help.");
            eb.AddField("Current Version", $"v{Utils.currentVersion}");
            //eb.AddField("DSharpPlus Version", $"v4.0.1");
            eb.AddField("Invite/Vote Link", "https://top.gg/bot/389513870835974146", true);
            eb.AddField("Support Server", "https://discord.gg/wHPwHu7");
            eb.AddField("GitHub", "https://github.com/Mrcarrot1/CarrotBot");
            await ctx.RespondAsync(embed: eb.Build());
        }
        [Command("setprefix"), Description("Sets the bot's prefix in this server."), RequirePermissions(Permissions.ManageGuild)]
        public async Task SetPrefix(CommandContext ctx, string prefix)
        {
            Database.Guilds[ctx.Guild.Id].GuildPrefix = prefix;
            await ctx.RespondAsync($"Set prefix for this server: `{prefix}`.");
        }
    }
}