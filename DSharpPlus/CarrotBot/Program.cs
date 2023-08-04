//Definitions for debugging
//BETA sets the bot to a beta state, which logs into the beta account, has more logging, and other tweaks.
//DATABASE_WRITE_PROTECTED is used for when the beta must read from the main database but cannot write to it. The only difference is that no data will be written to disk.
#define BETA
//#define DATABASE_WRITE_PROTECTED

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CarrotBot.Conversation;
using CarrotBot.Data;
using CarrotBot.Leveling;
using CarrotBot.SlashCommands;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;

namespace CarrotBot
{
    public static class Program
    {
        //Additional check added to make sure the binaries in production never run on the beta account
#if BETA
        public static readonly bool isBeta = Environment.UserName == "mrcarrot";
#else
        public static bool isBeta;
#endif

#if DATABASE_WRITE_PROTECTED
        public static readonly bool doNotWrite = true;
#else
        public static bool doNotWrite;
#endif


        public static bool conversation = false;
        private static bool _firstRun = true;

        public static DiscordShardedClient? discord;
        public static DiscordMember? Mrcarrot;
        public static DiscordGuild? BotGuild;
        public static string commandPrefix = "";
        static async Task Main(string[] args)
        {
#if !BETA
            if (args.Contains("--beta")) isBeta = true;
#endif

#if !DATABASE_WRITE_PROTECTED
            if (args.Contains("--db-write-protect")) doNotWrite = true;
#endif
            if (isBeta)
            {
                Logger.Log("Starting CarrotBot (using beta account)");
            }

            //Check for do not start flag in the form of a file-
            //The beta ignores this.
            if (File.Exists($@"{Utils.localDataPath}/DO_NOT_START.cb") && !isBeta)
            {
                Logger.Log("Do not start flag detected. Exiting.");
                Environment.Exit(0);
            }
            //Check to see if it's the beta, and set these values accordingly
            //I know I'll probably forget how this operator works at some point
            //So it's worth explaining the code
            string? token = isBeta ? SensitiveInformation.betaToken : SensitiveInformation.botToken;
            commandPrefix = isBeta ? "b%" : "%";
            Console.Title = isBeta ? "CarrotBot Beta" : "CarrotBot";


            discord = new DiscordShardedClient(new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
                MinimumLogLevel = isBeta ? LogLevel.Debug : LogLevel.Information,
                Intents = (DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers | DiscordIntents.MessageContents),
                LoggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new CBLoggerProvider()))
            });
            Database.Load();
            if (!isBeta)
                Dripcoin.LoadData();
            discord.MessageCreated += HandleCommand;
            discord.MessageCreated += HandleMessage;
            discord.Ready += Ready;
            discord.MessageUpdated += MessageUpdated;
            discord.MessageDeleted += MessageDeleted;
            discord.GuildMemberAdded += MemberJoined;
            discord.GuildCreated += GuildAdded;
            discord.GuildDeleted += GuildRemoved;
            discord.ClientErrored += HandleClientError;
            discord.ComponentInteractionCreated += HandleComponentInteraction;
            discord.ModalSubmitted += ModalHandler;
            discord.SocketErrored += HandleSocketError;
            //discord.MessageReactionAdded += ReactionAdded;


            List<string> stringPrefixes = new() { commandPrefix };
            if (!isBeta)
                stringPrefixes.Add("cb%");

            await discord.UseCommandsNextAsync(new CommandsNextConfiguration
            {
                StringPrefixes = stringPrefixes,
                EnableDefaultHelp = false,
                UseDefaultCommandHandler = false
            });
            Parallel.ForEach((await discord.GetCommandsNextAsync()).Values, commands =>
            {
                commands.RegisterCommands<CommandsNext.UngroupedCommands>();
                commands.RegisterCommands<CommandsNext.AdminCommands>();
                commands.RegisterCommands<CommandsNext.BotCommands>();
                commands.RegisterCommands<CommandsNext.MathCommands>();
                commands.RegisterCommands<CommandsNext.ServerCommands>();
                commands.RegisterCommands<CommandsNext.UserCommands>();
                commands.RegisterCommands<LevelingCommands>();
                commands.RegisterCommands<ConversationCommands>();
                commands.RegisterCommands<CommandsNext.JoinBlacklistCommands>();
                commands.RegisterCommands<CommandsNext.JoinFilterCommands>();
            });
            discord.GetShard(824824193001979924).GetCommandsNext().RegisterCommands<DripcoinCommands>();

            //await discord.UseSlashCommandsAsync();

            var slashCommands = await discord.UseSlashCommandsAsync();
            slashCommands.RegisterCommands<AdminCommands>();
            slashCommands.RegisterCommands<BotCommands>();
            slashCommands.RegisterCommands<CBGuildCommands>(388339196978266114);
            slashCommands.RegisterCommands<JoinBlacklistCommands>();
            slashCommands.RegisterCommands<JoinFilterCommands>();
            slashCommands.RegisterCommands<MathCommands>();
            slashCommands.RegisterCommands<ServerCommands>();
            slashCommands.RegisterCommands<UserCommands>();
            slashCommands.RegisterCommands<ConversationSlashCommands>();
            slashCommands.RegisterCommands<LevelingSlashCommands>();
            slashCommands.RegisterCommands<UngroupedCommands>();


            await discord.StartAsync();

            //Save the conversation message data every 5 minutes
            System.Timers.Timer? conversationSaveTimer = null;
            if (!isBeta)
            {
                conversationSaveTimer = new(300000); //Run every 5 minutes
                conversationSaveTimer.Elapsed += (sender, e) =>
                {
                    ConversationData.WriteMessageData();
                };
                conversationSaveTimer.AutoReset = true;
                conversationSaveTimer.Enabled = true;
            }
            await Task.Delay(-1);
            conversationSaveTimer?.Dispose();
        }
        static async Task MemberJoined(DiscordClient client, GuildMemberAddEventArgs e)
        {
            GuildData guildData = Database.GetOrCreateGuildData(e.Guild.Id);
            foreach (ulong roleId in guildData.RolesToAssignOnJoin)
            {
                await e.Member.GrantRoleAsync(e.Guild.GetRole(roleId));
            }
            foreach (JoinFilter filter in guildData.JoinFilters)
            {
                if (filter.Regex.IsMatch(e.Member.Username) && !filter.Exceptions.Contains(e.Member.Id))
                {
                    if (filter.Ban)
                    {
                        try
                        {
                            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
                            eb.WithColor(DiscordColor.Red);
                            eb.WithDescription($"You have been banned from {e.Guild.Name} by automatic username filter.");
                            await e.Member.SendMessageAsync(embed: eb.Build());
                        }
                        catch (UnauthorizedException) { } //Ignore this, it probably just means that the user's DMs are disabled
                        await e.Member.BanAsync(reason: "Username regex filter");
                    }
                    else
                    {
                        try
                        {
                            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
                            eb.WithColor(DiscordColor.Red);
                            eb.WithDescription($"You have been kicked from {e.Guild.Name} by automatic username filter.");
                            await e.Member.SendMessageAsync(embed: eb.Build());
                        }
                        catch (UnauthorizedException) { }
                        await e.Member.RemoveAsync(reason: "Username regex filter");
                    }
                }
            }
            foreach (JoinBlacklist blacklist in guildData.JoinBlacklists)
            {
                if (blacklist.Username == e.Member.Username && !blacklist.Exceptions.Contains(e.Member.Id))
                {
                    if (blacklist.Ban)
                    {
                        try
                        {
                            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
                            eb.WithColor(DiscordColor.Red);
                            eb.WithDescription($"You have been banned from {e.Guild.Name} by exact username blacklist.");
                            await e.Member.SendMessageAsync(embed: eb.Build());
                        }
                        catch (UnauthorizedException) { }
                        await e.Member.BanAsync(reason: "Username blacklisted");
                    }
                    else
                    {
                        try
                        {
                            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
                            eb.WithColor(DiscordColor.Red);
                            eb.WithDescription($"You have been kicked from {e.Guild.Name} by exact username blacklist.");
                            await e.Member.SendMessageAsync(embed: eb.Build());
                        }
                        catch (UnauthorizedException) { }
                        await e.Member.RemoveAsync(reason: "Username blacklisted");
                    }
                }
            }
        }
        static async Task HandleComponentInteraction(DiscordClient client, ComponentInteractionCreateEventArgs e)
        {
            string[] splitId = e.Id.Split('_');
            Console.WriteLine(e.Id);
            if (splitId[0] == "mmreplybutton")
            {
                if (ulong.TryParse(splitId[1], out ulong Id))
                {
                    try
                    {
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.Modal,
                        new DiscordInteractionResponseBuilder()
                        .WithTitle("Replying to Modmail")
                        .WithCustomId($"mmreply_{Id}")
                        .AddComponents(new TextInputComponent("Response", "response")))
                        .WaitAsync(TimeSpan.FromMinutes(15));
                    }
                    catch (Exception exc)
                    {
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"An error occurred: {exc.Message}").AsEphemeral());
                    }
                }
            }
            else if (splitId[0] == "mmfubutton")
            {
                if (splitId.Length >= 4 && ulong.TryParse(splitId[1], out ulong guildId) && ulong.TryParse(splitId[2], out ulong channelId) && ulong.TryParse(splitId[3], out ulong messageId))
                {
                    try
                    {
                        Console.WriteLine(guildId);
                        await e.Interaction.CreateResponseAsync(
                        InteractionResponseType.Modal,
                        new DiscordInteractionResponseBuilder()
                        .WithTitle("Responding to Moderators")
                        .WithCustomId($"mmfu_{guildId}_{channelId}_{messageId}")
                        .AddComponents(new TextInputComponent("Response", "response")))
                        .WaitAsync(TimeSpan.FromMinutes(15));
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine(exc.ToString());
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"An error occurred: {exc.Message}").AsEphemeral());
                    }
                }
            }
        }

        static async Task ModalHandler(DiscordClient client, ModalSubmitEventArgs e)
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            string[] splitId = e.Interaction.Data.CustomId.Split('_');
            if (splitId[0] == "mmreply")
            {
                if (ulong.TryParse(splitId[1], out ulong Id))
                {
                    try
                    {
                        DiscordMember member = await e.Interaction.Guild.GetMemberAsync(Id);
                        if (e.Values.TryGetValue("response", out string? message))
                        {
                            DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
                                .WithAuthor(name: $"Response from {e.Interaction.Guild.Name} Moderators", iconUrl: e.Interaction.Guild.IconUrl)
                                .WithDescription($"{message}")
                                .WithColor(Utils.CBOrange);

                            Regex URLRegex = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)");
                            foreach (Match match in URLRegex.Matches(message))
                            {
                                if (Utils.IsImageUrl(match.Value))
                                {
                                    eb.WithImageUrl(match.Value);
                                    break;
                                }
                            }

                            DiscordMessageBuilder messageBuilder = new DiscordMessageBuilder()
                                .WithEmbed(eb.Build())
                                .AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"mmfubutton_{e.Interaction.Guild.Id}_{e.Interaction.Channel.Id}_{(await e.Interaction.GetOriginalResponseAsync()).Id}", "Reply"));

                            eb = new DiscordEmbedBuilder()
                                .WithAuthor(name: $"Response sent by {e.Interaction.User.Username}#{e.Interaction.User.Discriminator}", iconUrl: e.Interaction.User.AvatarUrl)
                                .WithDescription(message)
                                .WithColor(Utils.CBOrange);

                            await member.SendMessageAsync(messageBuilder);

                            await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(eb.Build()));
                        }
                    }
                    catch (UnauthorizedException)
                    {
                        await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Failed to DM the user: Permission denied."));
                    }
                    catch (Exception exc)
                    {
                        await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"An error occurred: {exc.Message}"));
                    }
                }
            }
            else if (splitId[0] == "mmfu")
            {
                if (splitId.Length >= 4
                    && ulong.TryParse(splitId[1], out ulong guildId) && ulong.TryParse(splitId[2], out ulong channelId) && ulong.TryParse(splitId[3], out ulong messageId))
                {
                    try
                    {
                        DiscordChannel channel = await discord!.GetShard(guildId).GetChannelAsync(channelId);
                        DiscordMessage message = await channel.GetMessageAsync(messageId);

                        if (e.Values.TryGetValue("response", out string? response))
                        {
                            DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
                                .WithAuthor(name: $"Response from {e.Interaction.User.Username}#{e.Interaction.User.Discriminator}", iconUrl: e.Interaction.User.GetAvatarUrl(ImageFormat.Auto))
                                .WithDescription(response)
                                .WithColor(Utils.CBOrange);

                            Regex URLRegex = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)");
                            foreach (Match match in URLRegex.Matches(response))
                            {
                                if (Utils.IsImageUrl(match.Value))
                                {
                                    eb.WithImageUrl(match.Value);
                                    break;
                                }
                            }

                            DiscordMessageBuilder messageBuilder = new DiscordMessageBuilder()
                                .WithEmbed(eb.Build())
                                .AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"mmreplybutton_{e.Interaction.User.Id}", "Reply"));

                            await message.RespondAsync(messageBuilder);
                            await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Message sent."));
                        }
                    }
                    catch
                    {
                        await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("I couldn't send your message. Try contacting server moderators directly."));
                    }
                }
            }
        }
        static Task HandleClientError(DiscordClient client, ClientErrorEventArgs e)
        {
            if (e.EventName == "HearbeatFailure")
            {
                Process.Start($@"{Environment.CurrentDirectory}/CarrotBot");
                Environment.Exit(0);
            }
            return Task.CompletedTask;
        }
        static Task HandleSocketError(DiscordClient client, SocketErrorEventArgs e)
        {
            if (e.Exception.ToString().Contains(("HeartbeatFailure")))
            {
                Process.Start($@"{Environment.CurrentDirectory}/CarrotBot");
                Environment.Exit(0);
            }
            return Task.CompletedTask;
        }
        static async Task HandleMessage(DiscordClient client, MessageCreateEventArgs e)
        {
            try
            {
                if (e.Author.Id == discord!.CurrentUser.Id) return;
                if (e.Channel.IsPrivate) return;
                if (e.Author.IsBot) return;
                if (conversation)
                    await Conversation.Conversation.CarryOutConversation(e.Message);
                if (LevelingData.Servers.ContainsKey(e.Guild.Id))
                {
                    if (!LevelingData.Servers[e.Guild.Id].Users.ContainsKey(e.Author.Id))
                    {
                        LevelingData.Servers[e.Guild.Id].CreateUser(e.Message.Author.Id, DateTimeOffset.Now);
                    }
                    await LevelingData.Servers[e.Guild.Id].Users[e.Author.Id].HandleMessage(e.Message);
                }
                foreach (var user in e.MentionedUsers)
                {
                    GuildUserData mentionedUserData = Database.GetOrCreateGuildData(e.Guild.Id).GetOrCreateUserData(user.Id);
                    if (mentionedUserData.IsAFK)
                    {
                        await e.Channel.SendMessageAsync($"{user.Username} is AFK: {mentionedUserData.AFKMessage}\n({DateTimeOffset.Now.Subtract(mentionedUserData.AFKTime).ToString("g").Split('.')[0]} ago)");
                    }
                }
                GuildUserData userData = Database.GetOrCreateGuildData(e.Guild.Id).GetOrCreateUserData(e.Author.Id);
                if (userData.IsAFK)
                {
                    userData.RemoveAFK();
                    try
                    {
                        DiscordMember member = await e.Guild.GetMemberAsync(e.Author.Id);
                        await member.ModifyAsync(x =>
                        {
                            x.Nickname = member.Nickname.Replace("[AFK] ", "");
                        });
                        var message = await e.Channel.SendMessageAsync($"Welcome back <@{member.Id}>. I removed your AFK.");
                        await Task.Delay(5000);
                        await message.DeleteAsync();
                    }
                    catch (UnauthorizedException) { }
                }
                if (e.Author.Id == 366298290377195522 && e.Message.Content.ToLowerInvariant().Trim().Equals("am i right lads or am i right?"))
                {
                    await e.Channel.SendMessageAsync("<@!366298290377195522> You are right lad!");
                }
            }
            catch (Exception ee)
            {
                Logger.Log(ee.ToString(), Logger.CBLogLevel.EXC);
            }
        }
        static async Task Ready(DiscordClient client, ReadyEventArgs e)
        {
            try
            {
                BotGuild = await discord!.GetShard(388339196978266114).GetGuildAsync(388339196978266114);
                Mrcarrot = await BotGuild.GetMemberAsync(366298290377195522);
                Logger.Log("Connection ready");
                Utils.GuildCount = 0;
                foreach (DiscordClient shard in discord.ShardClients.Values)
                {
                    Utils.GuildCount += shard.Guilds.Count;
                }
                await discord.UpdateStatusAsync(new DiscordActivity($"in {Utils.GuildCount} servers | /help", ActivityType.Playing));

                if (_firstRun && !isBeta)
                {
                    await Conversation.Conversation.StartConversation(false);
                    _firstRun = false;
                }
                if (!doNotWrite)
                    LevelingData.LoadDatabase();
                //await BotGuild.GetChannel(502841234285527041).SendMessageAsync("CarrotBot ready.");
                foreach (GuildData guildData in Database.Guilds.Values)
                {
                    DiscordGuild guild;
                    try
                    {
                        guild = await discord.GetShard(guildData.Id).GetGuildAsync(guildData.Id);
                    }
                    catch (NotFoundException)
                    {
                        continue;
                    }
                    List<ulong> rolesToRemove = new(32);
                    foreach (KeyValuePair<ulong, ulong> role in guildData.CustomRoles)
                    {
                        DiscordMember? member = await guild.GetMemberAsync(role.Key);
                        if (member == null) rolesToRemove.Add(role.Key);
                        else
                        {
                            if (member.PremiumSince != null ||
                                guildData.CustomRolesAllowed != GuildData.AllowCustomRoles.Booster) continue;
                            DiscordRole? dRole = guild.GetRole(role.Value);
                            if (dRole == null)
                            {
                                continue;
                            }
                            rolesToRemove.Add(role.Key);
                            await member.RevokeRoleAsync(dRole,
                                "User has stopped boosting this server.");
                        }
                    }

                    foreach (ulong userId in rolesToRemove)
                    {
                        guildData.CustomRoles.Remove(userId);
                    }
                }
            }
            catch (Exception ee)
            {
                Logger.Log(ee.ToString(), Logger.CBLogLevel.EXC);
            }
        }
        static async Task MessageUpdated(DiscordClient client, MessageUpdateEventArgs e)
        {
            if (e.Channel.IsPrivate || e.Author.Id == discord!.CurrentUser.Id) return;
            if (!isBeta && ConversationData.ConversationMessagesByOrigId.TryGetValue(e.Message.Id, out var value))
            {
                await value.UpdateMessage();
            }

            GuildData guildData = Database.GetOrCreateGuildData(e.Guild.Id);
            /*
            Message Logs- Unfinished feature
            if (guildData.MessageLogsChannel != null)
            {
                DiscordChannel channel = e.Guild.GetChannel((ulong)guildData.MessageLogsChannel);
                DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
                    .WithAuthor($"Message sent by {e.Author.Username}#{e.Author.Discriminator} edited in #<{e.Channel.Id}>", iconUrl: e.Author.AvatarUrl)
                    .AddField("Original Contents", $"{e.MessageBefore.Content}");
            }*/
            if (guildData.MessageLogsChannel != null)
            {
                DiscordChannel channel = e.Guild.GetChannel((ulong)guildData.MessageLogsChannel);
                StringBuilder attachments1 = new(64);
                foreach (DiscordAttachment attachment in e.MessageBefore.Attachments)
                {
                    attachments1.Append($"\n{attachment.Url}");
                }
                StringBuilder attachments2 = new(64);
                foreach (DiscordAttachment attachment in e.Message.Attachments)
                {
                    attachments2.Append($"\n{attachment.Url}");
                }

                string a1str = attachments1.ToString().Trim();
                a1str = a1str == "" ? "None" : a1str;

                string a2str = attachments2.ToString().Trim();
                a2str = a2str == "" ? "None" : a2str;


                string contents1 = e.MessageBefore.Content;
                if (contents1.Length > 2036)
                    contents1 = contents1.SafeSubstring(0, 2033) + "...";

                string contents2 = e.Message.Content;
                if (contents2.Length > 2036)
                    contents2 = contents2.SafeSubstring(0, 2033) + "...";

                StringBuilder descriptionBuilder = new(1024);
                descriptionBuilder.Append($"Message Edited in **#{e.Channel.Name}**");
                descriptionBuilder.Append("\n**Before:** ");
                descriptionBuilder.Append(contents1);
                descriptionBuilder.Append("**\n\nAfter:** ");
                descriptionBuilder.Append(contents2);
                string description = descriptionBuilder.ToString();

                if (description.Length > 4096)
                {
                    description = description.SafeSubstring(0, 4093) + "...";
                }

                DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
                    .WithAuthor($"{e.Author.Username}#{e.Author.Discriminator} (Click to Jump)", $"{e.Message.JumpLink.AbsoluteUri}", iconUrl: e.Author.AvatarUrl)
                    .WithDescription(description)
                    .WithColor(Utils.DiscordYellow)
                    .WithFooter($"ID: {e.Author.Id}");

                if (a1str != "None" || a2str != "None")
                {
                    eb.AddField("Attachments Before", a1str);
                    eb.AddField("Attachments After", a2str);
                }
                await channel.SendMessageAsync(eb.Build());
            }
        }
        static async Task MessageDeleted(DiscordClient client, MessageDeleteEventArgs e)
        {
            if (e.Channel.IsPrivate || e.Message.Author == null || e.Message.Author.Id == discord!.CurrentUser.Id) return;
            if (ConversationData.ConversationMessagesByOrigId.TryGetValue(e.Message.Id, out var value))
            {
                await value.DeleteMessage(false);
            }
            GuildData guildData = Database.GetOrCreateGuildData(e.Guild.Id);
            if (guildData.MessageLogsChannel != null)
            {
                DiscordChannel channel = e.Guild.GetChannel((ulong)guildData.MessageLogsChannel);
                StringBuilder attachments = new(64);
                foreach (DiscordAttachment attachment in e.Message.Attachments)
                {
                    attachments.Append($"\n{attachment.Url}");
                }
                string astr = attachments.ToString().Trim();

                string description = $"**Message deleted in #{e.Channel.Name}**\n{e.Message.Content}";
                if (description.Length > 4096) description = description.SafeSubstring(0, 4093) + "...";

                DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
                    .WithAuthor($"{e.Message.Author.Username}#{e.Message.Author.Discriminator}", iconUrl: e.Message.Author.AvatarUrl)
                    .WithDescription(description)
                    .WithColor(Utils.DiscordRed)
                    .WithFooter($"ID: {e.Message.Author.Id}");

                if (astr != "")
                {
                    eb.AddField("Attachments", astr);
                }

                await channel.SendMessageAsync(eb.Build());
            }
        }
        static async Task HandleCommand(DiscordClient client, MessageCreateEventArgs e)
        {
            try
            {
                if (e.Author.IsBot) return;
                var cnext = client.GetCommandsNext();
                var msg = e.Message;
                var cmdStart = -1;
                //If beta, ignore any other prefix
                if (isBeta)
                {
                    cmdStart = msg.GetStringPrefixLength("b%");
                    if (cmdStart == -1)
                        cmdStart = msg.GetMentionPrefixLength(client.CurrentUser);
                }
                else
                {
                    if (!e.Channel.IsPrivate)
                    {
                        cmdStart = msg.GetStringPrefixLength(Database.GetOrCreateGuildData(e.Guild.Id).GuildPrefix);
                        //Special case for help command- the bot's status says %help, so you can run it like that anywhere
                        if (msg.Content.Trim().StartsWith($"{commandPrefix}help")) cmdStart = msg.GetStringPrefixLength(commandPrefix);

                        //Check for default prefixes if no guild-specific prefix was found
                        if (cmdStart == -1)
                        {
                            cmdStart = msg.GetStringPrefixLength("cb%");
                            if (cmdStart == -1)
                                cmdStart = msg.GetMentionPrefixLength(client.CurrentUser);
                        }
                    }
                    else
                    {
                        cmdStart = msg.GetStringPrefixLength(commandPrefix);

                        //Check for default prefixes if no guild-specific prefix was found
                        if (cmdStart == -1)
                        {
                            cmdStart = msg.GetStringPrefixLength("cb%");
                            if (cmdStart == -1)
                                cmdStart = msg.GetMentionPrefixLength(client.CurrentUser);
                        }
                        //If in DMs, check for all the prefixes first, but if none are found, interpret the message as a command anyway
                        if (cmdStart == -1)
                            cmdStart = 0;
                    }
                }

                //If no valid prefixes, exit
                if (cmdStart == -1) return;

                // Retrieve prefix.
                var prefix = msg.Content.Substring(0, cmdStart);

                // Retrieve full command string.
                var cmdString = msg.Content.Substring(cmdStart).Trim();

                var command = cnext.FindCommand(cmdString, out var args);

                string[] args2 = (args == null) ? Array.Empty<string>() : Utils.TokenizeString(args);
                int argsCount = args2.Length;

                Console.WriteLine($"User has entered command {command!.QualifiedName} with {argsCount} arguments:");
                for (int i = 0; i < argsCount; i++)
                {
                    Console.WriteLine(args2[i]);
                }

                if (command.Overloads.Any())
                {
                    if (!Utils.GetPossibleArgCounts(command).Contains(argsCount))
                    {
                        //If the user has entered an incorrect number of parameters, pretend they've just run %help <command>
                        if (command.Overloads.All(x => x.Arguments.Count != argsCount) && command.QualifiedName != "help")
                        {
                            if (cnext.RegisteredCommands.Any(x => cmdString.StartsWith(x.Value.QualifiedName)))
                            {
                                command = cnext.FindCommand($"help {command.QualifiedName}", out args);
                            }
                        }
                    }
                }

                var ctx = cnext.CreateContext(msg, prefix, command, args);

                await cnext.ExecuteCommandAsync(ctx);
            }
            catch (Exception ee)
            {
                Logger.Log(ee.ToString(), Logger.CBLogLevel.EXC);
            }
        }
        static async Task GuildAdded(DiscordClient client, GuildCreateEventArgs e)
        {
            await Task.Run(() =>
            {
                Utils.GuildCount++;
            });
        }
        static async Task GuildRemoved(DiscordClient client, GuildDeleteEventArgs e)
        {
            await Task.Run(() =>
            {
                Utils.GuildCount--;
                if (Database.Guilds.ContainsKey(e.Guild.Id))
                {
                    Database.DeleteGuildData(e.Guild.Id);
                }
                if (LevelingData.Servers.ContainsKey(e.Guild.Id))
                {
                    LevelingData.DeleteGuildData(e.Guild.Id);
                }
                if (ConversationData.ConversationChannels.Any(x => x.GuildId == e.Guild.Id))
                {
                    ConversationData.ConversationChannels.RemoveAll(x => x.GuildId == e.Guild.Id);
                    ConversationData.WriteDatabase();
                }
            });
        }
    }
}