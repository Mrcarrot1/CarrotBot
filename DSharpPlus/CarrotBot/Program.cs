//Definitions for debugging
//BETA sets the bot to a beta state, which logs into the beta account, has more logging, and other tweaks.
//DATABASE_WRITE_PROTECTED is used for when the beta must read from the main database but cannot write to it. The only difference is that no data will be written to disk.
//#define BETA
//#define DATABASE_WRITE_PROTECTED

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using CarrotBot.Leveling;
using CarrotBot.Data;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace CarrotBot
{
    class Program
    {
        //Additional check added to make sure the binaries in production never run on the beta account
#if BETA
        public static readonly bool isBeta = Environment.UserName == "mrcarrot";
#else
        public static readonly bool isBeta = false;
#endif

#if DATABASE_WRITE_PROTECTED
        public static readonly bool doNotWrite = true;
#else
        public static readonly bool doNotWrite = false;
#endif


        public static bool conversation = false;
        static bool firstRun = true;

        public static DiscordShardedClient discord;
        public static DiscordMember Mrcarrot;
        public static DiscordGuild BotGuild;
        public static string commandPrefix = "";
        static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        static async Task MainAsync(string[] args)
        {
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
            string token = isBeta ? SensitiveInformation.betaToken : SensitiveInformation.botToken;
            Console.Title = isBeta ? "CarrotBot Beta" : "CarrotBot";
            commandPrefix = isBeta ? "b%" : "%";


            discord = new DiscordShardedClient(new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
                MinimumLogLevel = isBeta ? Microsoft.Extensions.Logging.LogLevel.Debug : Microsoft.Extensions.Logging.LogLevel.Information,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers,
                LoggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new CBLoggerProvider()))
            });
            Database.Load();
            if (!isBeta)
                Dripcoin.LoadData();
            discord.MessageCreated += CommandHandler;
            discord.MessageCreated += (s, e) =>
            {
                _ = Task.Run(async () =>
                {
                    await MainMessageHandler(s, e);
                });

                return Task.CompletedTask;
            };
            discord.Ready += (s, e) =>
            {
                _ = Task.Run(async () =>
                {
                    await ReadyHandler(s, e);
                });

                return Task.CompletedTask;
            };
            discord.MessageUpdated += MessageUpdated;
            discord.MessageDeleted += MessageDeleted;
            discord.GuildMemberAdded += MemberJoined;
            discord.GuildCreated += GuildAdded;
            discord.GuildDeleted += GuildRemoved;
            discord.ClientErrored += HandleClientError;
            //discord.MessageReactionAdded += ReactionAdded;
            await discord.StartAsync();


            List<string> stringPrefixes = new List<string>();
            stringPrefixes.Add(commandPrefix);
            if (!isBeta)
                stringPrefixes.Add("cb%");

            await discord.UseCommandsNextAsync(new CommandsNextConfiguration
            {
                StringPrefixes = stringPrefixes,
                EnableDefaultHelp = false,
                UseDefaultCommandHandler = false
            });
            foreach (var commands in discord.GetCommandsNextAsync().Result)
            {
                commands.Value.RegisterCommands<Commands.UngroupedCommands>();
                commands.Value.RegisterCommands<Commands.AdminCommands>();
                commands.Value.RegisterCommands<Commands.BotCommands>();
                commands.Value.RegisterCommands<Commands.MathCommands>();
                commands.Value.RegisterCommands<Commands.ServerCommands>();
                commands.Value.RegisterCommands<Commands.UserCommands>();
                commands.Value.RegisterCommands<Leveling.LevelingCommands>();
                commands.Value.RegisterCommands<Conversation.ConversationCommands>();
                commands.Value.RegisterCommands<Commands.JoinBlacklistCommands>();
                commands.Value.RegisterCommands<Commands.JoinFilterCommands>();
            }
            discord.GetShard(824824193001979924).GetCommandsNext().RegisterCommands<DripcoinCommands>();

            //await discord.UseSlashCommandsAsync();

            //Save the conversation message data every 5 minutes
            if (!isBeta)
            {
                var conversationSaveTimer = new System.Threading.Timer(e => Conversation.ConversationData.WriteMessageData(), new AutoResetEvent(false), 18000000, 18000000); //Wait 5 minutes, then run every 5 minutes
            }
            await Task.Delay(-1);
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
                        catch { }
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
                        catch { }
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
                        catch { }
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
                        catch { }
                        await e.Member.RemoveAsync(reason: "Username blacklisted");
                    }
                }
            }
        }
        static async Task HandleMessage(DiscordClient client, MessageCreateEventArgs e)
        {
            await MainMessageHandler(client, e);
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
        static Task HandleSocketError(DiscordClient client, ClientErrorEventArgs e)
        {
            if (e.EventName == "HearbeatFailure")
            {
                Process.Start($@"{Environment.CurrentDirectory}/CarrotBot");
                Environment.Exit(0);
            }
            return Task.CompletedTask;
        }
        static async Task MainMessageHandler(DiscordClient client, MessageCreateEventArgs e)
        {
            Console.WriteLine($"{e.Author.Username}: {e.Message.Content}");
            try
            {
                if (e.Author.Id == discord.CurrentUser.Id) return;
                if (e.Channel.IsPrivate) return;
                if (e.Author.IsBot) return;
                if (conversation)
                    await Conversation.Conversation.CarryOutConversation(e.Message);
                if (LevelingData.Servers.ContainsKey(e.Guild.Id))
                {
                    if (LevelingData.Servers[e.Guild.Id].Users.ContainsKey(e.Author.Id))
                    {
                        await LevelingData.Servers[e.Guild.Id].Users[e.Author.Id].HandleMessage(e.Message);
                    }
                    else if (!e.Author.IsBot)
                    {
                        LevelingData.Servers[e.Guild.Id].CreateUser(e.Message.Author.Id, DateTimeOffset.Now);
                    }
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
                        await e.Guild.GetMemberAsync(e.Author.Id).Result.ModifyAsync(x =>
                        {
                            x.Nickname = e.Guild.GetMemberAsync(e.Author.Id).Result.Nickname.Replace("[AFK] ", "");
                        });
                    }
                    catch { }
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
            await ReadyHandler(client, e);
        }
        static async Task ReadyHandler(DiscordClient client, ReadyEventArgs e)
        {
            try
            {
                BotGuild = discord.GetShard(388339196978266114).GetGuildAsync(388339196978266114).Result;
                Mrcarrot = BotGuild.GetMemberAsync(366298290377195522).Result;
                Logger.Log("Connection ready");
                Utils.GuildCount = 0;
                foreach (DiscordClient shard in discord.ShardClients.Values)
                {
                    Utils.GuildCount += shard.Guilds.Count;
                }
                await discord.UpdateStatusAsync(new DiscordActivity($"in {Utils.GuildCount} servers | {commandPrefix}help", ActivityType.Playing));

                if (firstRun && !isBeta)
                {
                    await Conversation.Conversation.StartConversation(false);
                    firstRun = false;
                }
                if (!doNotWrite)
                    LevelingData.LoadDatabase();
                //await BotGuild.GetChannel(502841234285527041).SendMessageAsync("CarrotBot ready.");
            }
            catch (Exception ee)
            {
                Logger.Log(ee.ToString(), Logger.CBLogLevel.EXC);
            }
        }
        static async Task MessageUpdated(DiscordClient client, MessageUpdateEventArgs e)
        {
            if (Conversation.ConversationData.ConversationMessagesByOrigId.ContainsKey(e.Message.Id))
            {
                await Conversation.ConversationData.ConversationMessagesByOrigId[e.Message.Id].UpdateMessage();
            }
        }
        static async Task MessageDeleted(DiscordClient client, MessageDeleteEventArgs e)
        {
            if (Conversation.ConversationData.ConversationMessagesByOrigId.ContainsKey(e.Message.Id))
            {
                await Conversation.ConversationData.ConversationMessagesByOrigId[e.Message.Id].DeleteMessage(false);
            }
        }
        static async Task CommandHandler(DiscordClient client, MessageCreateEventArgs e)
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

                string[] args2 = (args == null) ? new string[0] : Utils.TokenizeString(args);
                int argsCount = (args2 == null) ? 0 : args2.Length;

                Console.WriteLine($"User has entered command {command.QualifiedName} with {argsCount} arguments:");
                for (int i = 0; i < argsCount; i++)
                {
                    Console.WriteLine(args2[i]);
                }

                if (command.Overloads.Any())
                {
                    if (!(((command.Overloads.Any(x => !x.Arguments.Any()) || command.Overloads.Any(x => x.Arguments.First().IsOptional)) && argsCount == 0) || command.Overloads.Any(x => x.Arguments.Last().IsCatchAll && x.Arguments.Count < argsCount)))
                    {
                        //If the user has entered an incorrect number of parameters, pretend they've just run %help <command>
                        if (!command.Overloads.Any(x => x.Arguments.Count == argsCount) && command.QualifiedName != "help")
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

                return;
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
                if (Leveling.LevelingData.Servers.ContainsKey(e.Guild.Id))
                {
                    Leveling.LevelingData.DeleteGuildData(e.Guild.Id);
                }
                if (Conversation.ConversationData.ConversationChannels.Any(x => x.GuildId == e.Guild.Id))
                {
                    Conversation.ConversationData.ConversationChannels.RemoveAll(x => x.GuildId == e.Guild.Id);
                    Conversation.ConversationData.WriteDatabase();
                }
            });
        }
    }
}