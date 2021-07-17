using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using CarrotBot.Leveling;
using CarrotBot.Data;

namespace CarrotBot
{
    class Program
    {
        //Set to true to run beta
        //Additional check added to make sure the binaries in production never run on the beta account
        public static readonly bool isBeta = Environment.UserName == "root" ? false : true;
        //Set to true to enable certain debug features such as more verbose logs
        private static readonly bool debug = true;

        public static bool conversation = false;
        static bool firstRun = true;

        public static DiscordClient discord;
        public static CommandsNextExtension commands;
        public static DiscordMember Mrcarrot;
        public static DiscordGuild BotGuild;
        public static string commandPrefix = "";
        static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        static async Task MainAsync(string[] args)
        {
            if(File.Exists($@"{Utils.localDataPath}/DO_NOT_START.cb"))
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
            

            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
                MinimumLogLevel = isBeta ? Microsoft.Extensions.Logging.LogLevel.Debug : Microsoft.Extensions.Logging.LogLevel.Information,
                Intents = DiscordIntents.All
            });
            Database.Load();
            Dripcoin.LoadData();
            discord.MessageCreated += HandleMessage;
            discord.MessageCreated += CommandHandler;
            discord.Ready += Ready;
            discord.MessageUpdated += MessageUpdated;
            discord.MessageDeleted += MessageDeleted;
            discord.GuildMemberAdded += MemberJoined;
            await discord.ConnectAsync();

            
            List<string> stringPrefixes = new List<string>();
            stringPrefixes.Add(commandPrefix);
            if(!isBeta)
                stringPrefixes.Add("cb%");

            commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = stringPrefixes,
                EnableDefaultHelp = false,
                UseDefaultCommandHandler = false
            });
            commands.RegisterCommands<Commands.UngroupedCommands>();
            commands.RegisterCommands<Conversation.ConversationCommands>();
            commands.RegisterCommands<Commands.AdminCommands>();
            commands.RegisterCommands<Commands.BotCommands>();
            commands.RegisterCommands<Commands.MathCommands>();
            commands.RegisterCommands<Commands.UserCommands>();
            commands.RegisterCommands<Commands.ServerCommands>();
            commands.RegisterCommands<LevelingCommands>();
            commands.RegisterCommands<DripcoinCommands>();

            await Task.Delay(-1);
        }
        static async Task MemberJoined(DiscordClient client, GuildMemberAddEventArgs e)
        {
            GuildData guildData = Database.GetOrCreateGuildData(e.Guild.Id);
            foreach(ulong roleId in guildData.RolesToAssignOnJoin)
            {
                await e.Member.GrantRoleAsync(e.Guild.GetRole(roleId));
            }
        }
        static async Task HandleMessage(DiscordClient client, MessageCreateEventArgs e)
        {
            try
            {
                if(debug)
                    Console.WriteLine($"{e.Author.Username}#{e.Author.Discriminator}: {e.Message.Content}");
                if(e.Author.Id == discord.CurrentUser.Id) return;
                if(e.Channel.IsPrivate) return;
                if(conversation)
                    await Conversation.Conversation.CarryOutConversation(e.Message);
                if(LevelingData.Servers.ContainsKey(e.Guild.Id))
                {
                    if(LevelingData.Servers[e.Guild.Id].Users.ContainsKey(e.Author.Id))
                    {
                        await LevelingData.Servers[e.Guild.Id].Users[e.Author.Id].HandleMessage(e.Message);
                    }
                    else if(!e.Author.IsBot)
                    {
                        LevelingData.Servers[e.Guild.Id].CreateUser(e.Message.Author.Id, DateTimeOffset.Now);
                    }
                }
                foreach(var user in e.MentionedUsers)
                {
                    GuildUserData mentionedUserData = Database.GetOrCreateGuildData(e.Guild.Id).GetOrCreateUserData(user.Id);
                    if(mentionedUserData.IsAFK)
                    {
                        await e.Channel.SendMessageAsync($"{user.Username} is AFK: {mentionedUserData.AFKMessage}\n({DateTimeOffset.Now.Subtract(mentionedUserData.AFKTime).ToString("g").Split('.')[0]} ago)");
                    }
                }
                GuildUserData userData = Database.GetOrCreateGuildData(e.Guild.Id).GetOrCreateUserData(e.Author.Id);
                if(userData.IsAFK)
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
                if(e.Author.Id == 366298290377195522 && e.Message.Content.ToLowerInvariant().Trim().Equals("am i right lads or am i right?"))
                {
                    await e.Channel.SendMessageAsync("<@!366298290377195522> You are right lad!");
                }
            }
            catch(Exception ee)
            {
                Logger.Log(ee.ToString(), Logger.LogLevel.EXC);
            }
        }
        static async Task Ready(DiscordClient client, ReadyEventArgs e)
        {
            try
            {
                    BotGuild = discord.GetGuildAsync(388339196978266114).Result;
                Mrcarrot = BotGuild.GetMemberAsync(366298290377195522).Result;
                Logger.Log("Connection ready");
                await discord.UpdateStatusAsync(new DiscordActivity($"in {client.Guilds.Count} servers | {commandPrefix}help", ActivityType.Playing));

                if(firstRun)
                {
                    await Conversation.Conversation.StartConversation(false);
                    firstRun = false;
                }
                LevelingData.LoadDatabase();
                //await BotGuild.GetChannel(502841234285527041).SendMessageAsync("CarrotBot ready.");
            }
            catch(Exception ee)
            {
                Logger.Log(ee.ToString(), Logger.LogLevel.EXC);
            }
            
        }
        static async Task MessageUpdated(DiscordClient client, MessageUpdateEventArgs e)
        {
            if(Conversation.ConversationData.ConversationMessagesByOrigId.ContainsKey(e.Message.Id))
            {
                await Conversation.ConversationData.ConversationMessagesByOrigId[e.Message.Id].UpdateMessage();
            }
        }
        static async Task MessageDeleted(DiscordClient client, MessageDeleteEventArgs e)
        {
            if(Conversation.ConversationData.ConversationMessagesByOrigId.ContainsKey(e.Message.Id))
            {
                await Conversation.ConversationData.ConversationMessagesByOrigId[e.Message.Id].DeleteMessage(false);
            }
        }
        static private Task CommandHandler(DiscordClient client, MessageCreateEventArgs e)
        {
            var cnext = client.GetCommandsNext();
            var msg = e.Message;
            var cmdStart = -1;
            //If beta, ignore any other prefix
            if(isBeta)
            {
                cmdStart = msg.GetStringPrefixLength("b%");
            }
            else
            {
                cmdStart = msg.GetStringPrefixLength(Database.Guilds[e.Guild.Id].GuildPrefix);
                //Special case for help command- the bot's status says %help, so you can run it like that anywhere
                if(msg.Content.Trim().StartsWith($"{commandPrefix}help")) cmdStart = msg.GetStringPrefixLength(commandPrefix);

                //Check for default prefixes if no guild-specific prefix was found
                if(cmdStart == -1)
                {
                    cmdStart = msg.GetStringPrefixLength("cb%");
                    if(cmdStart == -1)
                        cmdStart = msg.GetMentionPrefixLength(client.CurrentUser);
                } 
            }

            //If no valid prefixes, exit
            if(cmdStart == -1) return Task.CompletedTask; 

            // Retrieve prefix.
            var prefix = msg.Content.Substring(0, cmdStart);

            // Retrieve full command string.
            var cmdString = msg.Content.Substring(cmdStart);

            var command = cnext.FindCommand(cmdString, out var args);

            var ctx = cnext.CreateContext(msg, prefix, command, args);
            
            Task.Run(async () => await cnext.ExecuteCommandAsync(ctx));

            return Task.CompletedTask;
        }
    }
}
