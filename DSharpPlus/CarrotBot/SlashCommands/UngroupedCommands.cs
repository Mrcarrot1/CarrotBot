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
                        if (candidateCommand == null || !candidateCommand.ExecutionChecks.Any())
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
        eb.WithDescription($"CarrotBot is a multipurpose Discord bot made by Mrcarrot#3305. Use `{Program.commandPrefix}help` for command help.");
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
    [SlashCommand("deletemydata", "Used to delete all data about your account that CarrotBot has stored.")]
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
    public async Task Modmail(InteractionContext ctx, [Option("message", "The message to send. Please include any images as links.")] string message)
    {
        await ctx.IndicateResponseAsync(true);
        GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
        if (guildData.ModMailChannel != null)
        {
            DiscordChannel channel = ctx.Guild.GetChannel((ulong)guildData.ModMailChannel);
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
                .WithAuthor($"Modmail from {ctx.Member.Username}#{ctx.Member.Discriminator}", iconUrl: ctx.Member.AvatarUrl)
                .WithDescription($"{message}")
                .WithFooter($"ID: {ctx.Member.Id}")
                .WithColor(Utils.CBOrange);

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
}

/*namespace CarrotBot.SlashCommands
{
    public class UngroupedCommands : ApplicationCommandModule
    {
        [SlashCommand("help", "Displays command help."), DescriptionLocalization(Localization.AmericanEnglish, "Displays command help.")]
        public async Task Help(InteractionContext ctx, [DescriptionLocalization(Localization.AmericanEnglish, "Command to provide help for.")] params string[] command)
        {
            string basicDescription = $"Listing top-level commands and groups. Use `{Program.commandPrefix}help <command/group/module>` to see subcommands or usage details.\nModules are commands that do not share a prefix(such as `conversation acceptterms`, `conversation addchannel`, etc.) but are related in function.";
            if (!ctx.Channel.IsPrivate)
            {
                GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
                if (guildData.GuildPrefix != Program.commandPrefix)
                    basicDescription = $"Listing top-level commands and groups. Use `{guildData.GuildPrefix}help <command/group/module>` to see subcommands or usage details.\nModules are commands that do not share a prefix(such as `conversation acceptterms`, `conversation addchannel`, etc.) but are related in function.\nThis server's prefix is `{guildData.GuildPrefix}`. You can also use the prefix `cb%` or <@!{Program.discord.CurrentUser.Id}>.";
            }
            try
            {
                var topLevel = ctx.CommandsNext.RegisteredCommands.Distinct();
                DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
                eb.WithTitle("Help");
                eb.WithColor(Utils.CBGreen);


                if (command != null && command.Any())
                {
                    var searchIn = new List<Command>();
                    foreach (var c in topLevel)
                    {
                        searchIn.Add(c.Value);
                    }
                    searchIn = searchIn.OrderBy(x => x.Name).ToList().GroupBy(x => x.QualifiedName).Select(x => x.First()).ToList();
                    if (command.First().ToLower().Trim() == "leveling")
                    {
                        eb.WithDescription("Showing all commands in module `leveling`.");
                        var levelingCommands = searchIn.Where(x => x.CustomAttributes.Any(x => x.GetType() == typeof(Leveling.LevelingCommandAttribute))).Where(x => !x.IsHidden);
                        List<Command> eligibleCommands = new List<Command>();
                        foreach (var candidateCommand in levelingCommands)
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
            catch (Exception e)
            {
                Logger.Log(e.ToString(), Logger.CBLogLevel.EXC);
            }
        }
        [Command("shutdown"), Hidden, RequireOwner]
        public async Task Shutdown(CommandContext ctx, bool dontRestart = false)
        {
            if (dontRestart)
            {
                File.WriteAllText($@"{Utils.localDataPath}/DO_NOT_START.cb", "DO_NOT_START");
            }
            await ctx.RespondAsync("CarrotBot shutting down. Good night!");
            Database.FlushDatabase(true);
            Leveling.LevelingData.FlushAllData();
            Dripcoin.WriteData();
            Conversation.ConversationData.WriteDatabase();
            Logger.Log($"Bot shutdown initiated by {ctx.User.Username}#{ctx.User.Discriminator}.");
            Console.WriteLine();
            Environment.Exit(0);
        }
        [Command("reload"), Hidden, RequireOwner]
        public async Task ReloadData(CommandContext ctx)
        {
            Conversation.ConversationData.LoadDatabase();
            Leveling.LevelingData.LoadDatabase();
            Database.Load();
            await ctx.RespondAsync("Reloaded all data.");
        }
        [Command("restart"), Hidden, RequireOwner]
        public async Task Restart(CommandContext ctx)
        {
            if (ctx.User.Id != 366298290377195522) return;
            await ctx.RespondAsync("CarrotBot restarting. Give me a minute...");
            Logger.Log("Bot restarting.");
            Database.FlushDatabase(true);
            Process.Start($@"{Environment.CurrentDirectory}/CarrotBot");
            Console.WriteLine();
            Environment.Exit(0);
        }
        [Command("updateping"), Hidden, RequireGuild]
        public async Task UpdatePing(CommandContext ctx)
        {
            if (!ctx.Guild.Equals(Program.BotGuild)) return;
            DiscordRole role = ctx.Guild.Roles.FirstOrDefault(x => x.Value.Name == "Updoot Ping").Value;
            if (!ctx.Member.Roles.ToList().Contains(role))
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
        public async Task AFK(CommandContext ctx, [RemainingText, Description("The message to set.")] string message = "AFK")
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
                responseString = responseString.Substring(1, responseString.Length - 2); //This API gives response strings surrounded by [] so we remove them
                Console.WriteLine("Response string: {0}", responseString);
                KONNode node = KONParser.Default.ParseJSON(responseString);
                string url = (string)node.Values["url"];
                await ctx.RespondAsync(url);
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString(), Logger.CBLogLevel.EXC);
            }
        }
        [Command("about"), Description("Shows various information about the bot.")]
        public async Task About(CommandContext ctx)
        {
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle("About CarrotBot");
            eb.WithColor(Utils.CBGreen);
            eb.WithDescription($"CarrotBot is a multipurpose Discord bot made by Mrcarrot#3305. Use `{Program.commandPrefix}help` for command help.");
            eb.AddField("Shards", $"{Program.discord.ShardClients.Count}", true);
            eb.AddField("Guilds", $"{Utils.GuildCount}", true);
            eb.AddField("Current Version", $"v{Utils.currentVersion}", true);
            //eb.AddField("DSharpPlus Version", $"v4.0.1");
            eb.AddField("Invite/Vote Link", "https://discord.bots.gg/bots/389513870835974146");
            eb.AddField("Support Server", "https://discord.gg/wHPwHu7");
            eb.AddField("Source Repository", "https://github.com/Mrcarrot1/CarrotBot");
            eb.AddField("Website", "https://carrotbot.calebmharper.com");
            await ctx.RespondAsync(embed: eb.Build());
        }
        [Command("setprefix"), Description("Sets the bot's prefix in this server."), RequirePermissions(Permissions.ManageGuild)]
        public async Task SetPrefix(CommandContext ctx, string prefix)
        {
            GuildData guildData = Database.GetOrCreateGuildData(ctx.Guild.Id);
            guildData.GuildPrefix = prefix;
            guildData.FlushData();
            await ctx.RespondAsync($"Set prefix for this server: `{prefix}`.");
        }
        [Command("deletemydata")]
        public async Task DeleteUserData(CommandContext ctx, bool confirm = false)
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
    }
}*/