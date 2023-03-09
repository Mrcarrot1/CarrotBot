using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using CarrotBot.Data;
using KarrotObjectNotation;

namespace CarrotBot.SlashCommands;
public class UngroupedCommands : ApplicationCommandModule
{
    [SlashCommand("help", "Command help.")]
    public async Task Help(InteractionContext ctx)
    {
        await ctx.IndicateResponseAsync();
        DiscordEmbedBuilder eb = new();
        eb.WithTitle("Help")
            .WithColor(Utils.CBGreen)
            .WithDescription($"To see available slash commands, you can view slash commands in the list available in the Discord client. To see help for text commands, use {Program.commandPrefix}help.\n\nThis command is currently a work in progress. In the future, as more slash command options are made available, it will be improved.");
        await ctx.UpdateResponseAsync(eb.Build());
    }

    /*[SlashCommand("help", "Displays command help.")]
    public async Task Help(InteractionContext ctx, [Option("command", "The command to show help for.")] string command = null)
    {
        await ctx.IndicateResponseAsync();
        string basicDescription = $"Listing top-level slash commands and groups. Use `/help <command/group/module` to see subcommands or usage details.\nModules are commands that do not share a prefix(such as `conversation acceptterms`, `conversation addchannel`, etc.) but are related in function.\nCarrotBot's slash command support is currently in beta. To see text-based commands, use %help.";
        string[] commandTokens = command != null ? command.Split(' ') : null;
        try
        {
            var topLevel = ctx.SlashCommandsExtension.RegisteredCommands.Distinct();
            DiscordEmbedBuilder eb = new();
            eb.WithTitle("Help");
            eb.WithColor(Utils.CBGreen);

            if (commandTokens != null && commandTokens.Any())
            {
                var searchIn = new List<DiscordApplicationCommand>();
                foreach (var c in topLevel)
                {
                    searchIn.Add(c.Value.First());
                }
                searchIn = searchIn.OrderBy(x => x.Name).ToList();
                if (commandTokens.First().ToLower().Trim() == "leveling")
                {
                    eb.WithDescription("Showing all commands in module `leveling`.");
                    var levelingCommands = searchIn.Where(x => x.GetType().GetCustomAttributes(false).Any(x => x.GetType() == typeof(Leveling.LevelingCommandAttribute)));
                    List<DiscordApplicationCommand> eligibleCommands = new List<DiscordApplicationCommand>();
                    foreach (var candidateCommand in levelingCommands)
                    {
                        if (candidateCommand == null || candidateCommand.DefaultMemberPermissions == Permissions.None)
                        {
                            eligibleCommands.Add(candidateCommand);
                            continue;
                        }

                        //Check user permissions and whether this is a DM channel
                        bool permissionsValid = false;
                        if (ctx.Channel.IsPrivate) 
                            permissionsValid = candidateCommand.DefaultMemberPermissions == Permissions.None;
                        else 
                            permissionsValid = (ctx.Member.Permissions & candidateCommand.DefaultMemberPermissions) == (Permissions.All & candidateCommand.DefaultMemberPermissions);

                        if (permissionsValid)
                            eligibleCommands.Add(candidateCommand);
                    }
                    if (eligibleCommands.Any())
                    {
                        string subcommands = "None";
                        foreach (Command c in eligibleCommands)
                        {
                            if (subcommands == "None")
                                subcommands = $"`{c.QualifiedName}`";
                            else
                                subcommands += $", `{c.QualifiedName}`";
                        }
                        eb.AddField("Commands", subcommands);
                    }
                    //eb.AddField("Settings Helper", "Configuring leveling using individual commands not your speed? Try out the new [leveling settings helper](https://carrotbot.calebmharper.com/LevelingHelper) beta!");
                    await ctx.RespondAsync(eb.Build());
                    return;
                }
                Command cmd = null;
                //Have to use 1-indexing for the overload so the user can enter the index correctly
                int overload = 1;
                foreach (var c in command)
                {
                    if (searchIn == null)
                    {
                        if (int.TryParse(c, out overload)) break;
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
                        foreach (Command c in eligibleCommands)
                        {
                            if (subcommands == "None")
                                subcommands = $"`{c.QualifiedName}`";
                            else
                                subcommands += $", `{c.QualifiedName}`";
                        }
                        eb.AddField("Subcommands", subcommands);
                    }
                }
                else if (cmd != null)
                {
                    eb.WithDescription($"`{cmd.QualifiedName}`:" +
                    $"{cmd.Description}");
                    if (cmd.Description == null)
                    {
                        eb.WithDescription($"`{cmd.QualifiedName}`");
                    }
                    if (cmd.Overloads.Count > 1)
                    {
                        eb.WithFooter($"This command has {cmd.Overloads.Count} overloads. To see them, use `%help {cmd.QualifiedName} <overload number>`.");
                    }
                    if (cmd.Overloads.Count < overload)
                    {
                        eb.WithDescription($"The overload you have specified does not exist. Command `{cmd.QualifiedName}` has {cmd.Overloads.Count} overloads.");
                    }
                    else if (cmd.Overloads[overload - 1].Arguments.Any()) //Have to use -1 to convert to 0 indexing
                    {
                        string Overloads = "";
                        foreach (var arg in cmd.Overloads[overload - 1].Arguments)
                        {
                            string argstr = "";
                            if (arg.IsOptional)
                            {
                                argstr = $"`[{arg.Name}]: {Utils.GetUserFriendlyTypeName(arg.Type)}`: {arg.Description} Default Value: {arg.DefaultValue}";
                                if (arg.DefaultValue == null)
                                    argstr = argstr.Replace("Default Value: ", "Default Value: Empty");
                            }
                            else argstr = $"`<{arg.Name}>: {Utils.GetUserFriendlyTypeName(arg.Type)}`: {arg.Description}";
                            if (arg.IsCatchAll)
                            {
                                argstr = argstr
                                .Replace($"{arg.Name}>", $"{arg.Name}...>")
                                .Replace($"{arg.Name}]", $"{arg.Name}...]");
                            }
                            if (arg.Description == null)
                            {
                                argstr = argstr
                                .Replace("`: ", "`");
                            }
                            Overloads += $"\n{argstr}";
                        }
                        eb.AddField("Parameters", Overloads.Trim());
                    }
                    else
                    {
                        eb.AddField("Parameters", "*None.*");
                    }
                }
                else
                {
                    eb.WithDescription("*Command not found.*");
                }
            }
            //List all commands if no input
            else
            {
                eb.WithDescription(basicDescription);

                string topLevelCommands = "None";
                string commandGroups = "None";
                string modules = "`leveling`";
                foreach (KeyValuePair<string, Command> command1 in topLevel.OrderBy(x => x.Value.Name))
                {
                    Command cmd = command1.Value;

                    var candidateFailedChecks = await cmd.RunChecksAsync(ctx, true).ConfigureAwait(false);
                    if (candidateFailedChecks.Any())
                        continue;

                    if (!ctx.Channel.IsPrivate)
                    {
                        if ((cmd.Name == "rank" || cmd.Name == "leaderboard" || cmd.Name == "disableleveling") && !Leveling.LevelingData.Servers.ContainsKey(ctx.Guild.Id)) continue;
                        if (cmd.Name == "enableleveling" && Leveling.LevelingData.Servers.ContainsKey(ctx.Guild.Id)) continue;
                        if (cmd.IsHidden && !(cmd.Name == "dripcoin" && ctx.Guild.Id == 824824193001979924))
                        {
                            continue;
                        }
                    }

                    if (cmd is CommandGroup group)
                    {
                        if (commandGroups.Contains($"`{cmd.Name}`")) continue;
                        if (commandGroups == "None")
                            commandGroups = $"`{cmd.Name}`";
                        else if (!commandGroups.Contains($"`{cmd.Name}`"))
                            commandGroups += $", `{cmd.Name}`";
                    }
                    else
                    {
                        if (topLevelCommands == "None")
                            topLevelCommands = $"`{cmd.Name}`";
                        else if (!topLevelCommands.Contains($"`{cmd.Name}`"))
                            topLevelCommands += $", `{cmd.Name}`";
                    }
                }
                eb.AddField("Commands", topLevelCommands);
                eb.AddField("Groups", commandGroups);
                eb.AddField("Modules", modules);
            }


            await ctx.RespondAsync(embed: eb.Build());
        }
        catch
        {

        }

        await ctx.RespondEmbedAsync(
                new DiscordEmbedBuilder()
                .WithDescription("fuck slash commands")
                .WithTitle("I hate slash commands so fucking much")

                .Build()
                );
    }*/

    [SlashCommand("shutdown", "Shuts the bot down.", false), SlashRequireOwner]
    public async Task Shutdown(InteractionContext ctx, [Option("dontRestart", "Whether or not to prevent the bot from automatically restarting.")] bool dontRestart = false)
    {
        await ctx.IndicateResponseAsync();
        if (dontRestart)
        {
            File.WriteAllText($@"{Utils.localDataPath}/DO_NOT_START.cb", "DO_NOT_START");
        }
        await ctx.UpdateResponseAsync("CarrotBot shutting down. Good night!");
        Database.FlushDatabase(true);
        Leveling.LevelingData.FlushAllData();
        if (!Program.isBeta)
        {
            Conversation.ConversationData.WriteDatabase();
            Dripcoin.WriteData();
        }
        Logger.Log($"Bot shutdown initiated by {ctx.User.Username}#{ctx.User.Discriminator}.");
        Console.WriteLine();
        Environment.Exit(0);
    }

    [SlashCommand("reload", "Reloads the bot's database.", false), SlashRequireOwner]
    public async Task ReloadData(InteractionContext ctx)
    {
        await ctx.IndicateResponseAsync();
        if (!Program.isBeta)
            Conversation.ConversationData.LoadDatabase();
        Leveling.LevelingData.LoadDatabase();
        Database.Load();
        await ctx.UpdateResponseAsync("Reloaded all data.");
    }

    [SlashCommand("restart", "Restarts the bot.", false), SlashRequireOwner]
    public async Task Restart(InteractionContext ctx)
    {
        if (ctx.User.Id != 366298290377195522) return;
        await ctx.RespondAsync("CarrotBot restarting. Give me a minute...");
        Logger.Log("Bot restarting.");
        Database.FlushDatabase(true);
        if (!Program.isBeta)
        {
            Conversation.ConversationData.WriteDatabase();
            Dripcoin.WriteData();
        }
        Process.Start($@"{Environment.CurrentDirectory}/CarrotBot");
        Console.WriteLine();
        Environment.Exit(0);
    }

    [SlashCommand("afk", "Sets your AFK message.")]
    public async Task AFK(InteractionContext ctx, [Option("message", "The message to set.")] string message = "AFK")
    {
        await ctx.IndicateResponseAsync();
        GuildUserData userData = Database.GetOrCreateGuildData(ctx.Guild.Id).GetOrCreateUserData(ctx.User.Id);
        userData.SetAFK(message);
        try
        {
            await ctx.Member.ModifyAsync(x => x.Nickname = $"[AFK] {ctx.Member.DisplayName}");
        }
        catch { }
        await ctx.UpdateResponseAsync($"Set your AFK: {message}");
    }
    private static readonly HttpClient client = new HttpClient();
    [SlashCommand("catpic", "Provides a random cat picture courtesy of thecatapi.com")]
    public async Task CatPic(InteractionContext ctx)
    {
        await ctx.IndicateResponseAsync();
        try
        {
            client.DefaultRequestHeaders.Add("x-api-key", SensitiveInformation.catAPIKey);
            var responseString = await client.GetStringAsync("https://api.thecatapi.com/v1/images/search");
            responseString = responseString.Substring(1, responseString.Length - 2); //This API gives response strings surrounded by [] so we remove them
            Console.WriteLine("Response string: {0}", responseString);
            KONNode node = KONParser.Default.ParseJSON(responseString);
            string url = (string)node.Values["url"];
            await ctx.UpdateResponseAsync(url);
        }
        catch (Exception e)
        {
            await ctx.UpdateResponseAsync("Something went wrong. Please try again.");
            Logger.Log(e.ToString(), Logger.CBLogLevel.EXC);
        }
    }
    [SlashCommand("about", "Shows various information about the bot.")]
    public async Task About(InteractionContext ctx)
    {
        await ctx.IndicateResponseAsync();
        DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
        eb.WithTitle("About CarrotBot");
        eb.WithColor(Utils.CBGreen);
        eb.WithDescription($"CarrotBot is a multipurpose Discord bot made by Mrcarrot#3305. Use `/help` for command help.");
        eb.AddField("Shards", $"{Program.discord.ShardClients.Count}", true);
        eb.AddField("Guilds", $"{Utils.GuildCount}", true);
        eb.AddField("Current Version", $"v{Utils.currentVersion}", true);
        //eb.AddField("DSharpPlus Version", $"v4.0.1");
        eb.AddField("Invite/Vote Link", "https://discord.bots.gg/bots/389513870835974146");
        eb.AddField("Support Server", "https://discord.gg/wHPwHu7");
        eb.AddField("Source Repository", "https://github.com/Mrcarrot1/CarrotBot");
        eb.AddField("Website", "https://carrotbot.calebmharper.com");
        await ctx.UpdateResponseAsync(eb.Build());
    }
    [SlashCommand("setprefix", "Sets the bot's text command prefix in this server."), SlashRequirePermissions(Permissions.ManageGuild)]
    public async Task SetPrefix(InteractionContext ctx, [Option("prefix", "The prefix to set.")] string prefix)
    {
        GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
        guildData.GuildPrefix = prefix;
        guildData.FlushData();
        await ctx.RespondAsync($"Set prefix for this server: `{prefix}`.");
    }
    [SlashCommand("delete-my-data", "Used to delete all data about your account that CarrotBot has stored.")]
    public async Task DeleteUserData(InteractionContext ctx, [Option("confirm", "Whether or not to confirm deletion.")] bool confirm = false)
    {
        if (!confirm)
        {
            await ctx.RespondAsync("This command is used to request full deletion of all data CarrotBot has stored pertaining to your account.\nTo confirm that you wish to do this, please retype this command and add `true` after.");
        }
        else
        {
            await ctx.RespondAsync("You have requested a full deletion of all data CarrotBot has stored pertaining to your account. Please note that this may take some time and that CarrotBot may store additional data in the future.\nHowever, the bot does not store global user data, so data pertaining to you will not be stored unless you share a server with the bot.");
            Leveling.LevelingData.DeleteUserData(ctx.User.Id);
            Database.DeleteUserData(ctx.User.Id);
        }
    }
    [SlashCommand("modmail", "Sends a message to server moderators.")]
    public async Task Modmail(InteractionContext ctx, [Option("message", "The message to send. You can attach up to one image, or link to multiple in this message.")] string message, [Option("attachment", "An image or file to add to your message.")] DiscordAttachment attachment = null)
    {
        if (ctx.Channel.IsPrivate)
        {
            await ctx.RespondAsync("This command must be used in a server. Please go to the server which you wish to contact and use the command there.");
            return;
        }
        await ctx.IndicateResponseAsync(true);
        GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
        if (guildData.ModMailChannel != null)
        {
            DiscordChannel channel = ctx.Guild.GetChannel((ulong)guildData.ModMailChannel);
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
                .WithAuthor($"Modmail from {ctx.Member.Username}#{ctx.Member.Discriminator}", iconUrl: ctx.Member.AvatarUrl)
                .WithDescription($"{message}")
                .WithFooter($"ID: {ctx.Member.Id}")
                .WithColor(Utils.CBOrange)
                .AddField("User Mention", $"<@!{ctx.Member.Id}>");

            if (attachment != null)
            {
                if (Utils.IsImageUrl(attachment.Url))
                {
                    eb.WithImageUrl(attachment.Url);
                }
                else
                {
                    eb.AddField("Attached File", attachment.Url);
                }
            }
            else
            {
                //Check for image URLs and set the first one we find to the embed's image URL
                Regex URLRegex = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)");
                foreach (Match match in URLRegex.Matches(message))
                {
                    if (Utils.IsImageUrl(match.Value))
                    {
                        eb.WithImageUrl(match.Value);
                        break;
                    }
                }
            }

            //var sentMessage = await channel.SendMessageAsync(embed: eb.Build());
            var button = new DiscordButtonComponent(ButtonStyle.Primary, $"mmreplybutton_{ctx.User.Id}", "Reply");

            var messageBuilder = new DiscordMessageBuilder()
                .WithEmbed(eb.Build())
                .AddComponents(button);

            await channel.SendMessageAsync(messageBuilder);

            await ctx.UpdateResponseAsync("Message sent.");
        }
        else
        {
            await ctx.UpdateResponseAsync("This server does not currently have modmail configured. Please try contacting the server's moderators directly.");
        }
    }
    //[SlashCommand("custom-role", "Grants a custom role(in future, to server boosters).")]
    public async Task CustomRole(InteractionContext ctx, [Option("name", "The name of the role.")] string name,
    //Discord default role colors
    [Choice("Light Teal", "#1abc9c")]
    [Choice("Dark Teal", "#11806a")]
    [Choice("Light Green", "#2ecc71")]
    [Choice("Dark Green", "#1f8b4c")]
    [Choice("Light Blue", "#3498db")]
    [Choice("Dark Blue", "#206694")]
    [Choice("Light Purple", "#9b59b6")]
    [Choice("Dark Purple", "#71368a")]
    [Choice("Light Pink", "#e91e63")]
    [Choice("Dark Pink", "#ad1457")]
    [Choice("Light Yellow", "#f1c40f")]
    [Choice("Dark Yellow", "#c27c0e")]
    [Choice("Light Orange", "#e67e22")]
    [Choice("Dark Orange", "#a84300")]
    [Choice("Light Red", "#e74c3c")]
    [Choice("Dark Red", "#992d22")]
    [Choice("Light Grey 1", "#95a5a6")]
    [Choice("Light Grey 2", "#979c9f")]
    [Choice("Dark Grey 1", "#607d8b")]
    [Choice("Dark Grey 2", "#546e7a")]
    [Option("color", "The hex color for the role."),
    NameLocalization(Localization.BritishEnglish, "colour"),
    DescriptionLocalization(Localization.BritishEnglish, "The hex colour for the role.")] string color, [Option("icon", "The URL or link to your desired icon image.")] string iconUrl = null)
    {
        try
        {
            GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
            //if (guildData.CustomRolesAllowed == GuildData.AllowCustomRoles.All || (guildData.CustomRolesAllowed == GuildData.AllowCustomRoles.Booster && ctx.Member.PremiumSince is not null))
            {
                await ctx.IndicateResponseAsync(true);
                if (!Regex.IsMatch(color, "#?[0-9A-Fa-f]{6}"))
                {
                    await ctx.UpdateResponseAsync("Please enter a valid hex color. To find your desired color value, visit ");
                    return;
                }
                Stream iconStream = null;
                if (iconUrl != null)
                {
                    if (!Utils.IsImageUrl(iconUrl))
                    {
                        await ctx.UpdateResponseAsync("Please provide a valid image URL!");
                        return;
                    }
                    HttpClient client = new();
                    if (!Directory.Exists("temp")) Directory.CreateDirectory("temp");
                    var httpIconStream = await client.GetStreamAsync(iconUrl);

                }
                DiscordRole role = await ctx.Guild.CreateRoleAsync(name, color: new DiscordColor(color), icon: iconStream);
                DiscordRole headerRole = ctx.Guild.Roles[1044292868329705562];
                await role.ModifyPositionAsync(headerRole.Position - 1); //For some unfathomable reason position - 1 is one *lower*. I have no idea why the Discord devs did this or what they were smoking.
                await ctx.Member.GrantRoleAsync(role);
                await ctx.UpdateResponseAsync("Role granted.");
            }
            /*else if (guildData.CustomRolesAllowed == GuildData.AllowCustomRoles.Booster)
            {
                await ctx.RespondAsync("Custom roles are only available to people who boost!\nBoost to help the server and get a custom role!", true);
            }
            else
            {
                await ctx.RespondAsync("This server doesn't have custom roles enabled.", true);
            }*/
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
}