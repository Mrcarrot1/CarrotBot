using System;
using System.Threading.Tasks;
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
        public static readonly bool isBeta = false;
        //Set to true to enable certain debug features such as more verbose logs
        private static readonly bool debug = true;

        public static bool conversation = false;
        static bool firstRun = true;

        public static DiscordClient discord;
        public static CommandsNextModule commands;
        public static DiscordMember Mrcarrot;
        public static DiscordGuild BotGuild;
        private static string commandPrefix = "";
        static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        static async Task MainAsync(string[] args)
        {
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
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug
            });
            Database.Load();
            Dripcoin.LoadData();
            discord.MessageCreated += HandleMessage;
            discord.Ready += Ready;
            discord.MessageUpdated += MessageUpdated;
            discord.MessageDeleted += MessageDeleted;
            discord.GuildMemberAdded += MemberJoined;
            await discord.ConnectAsync();

            
            

            commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefix = commandPrefix,
                //EnableDefaultHelp = false
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
        static async Task MemberJoined(GuildMemberAddEventArgs e)
        {
            GuildData guildData = Database.GetOrCreateGuildData(e.Guild.Id);
            foreach(ulong roleId in guildData.RolesToAssignOnJoin)
            {
                await e.Member.GrantRoleAsync(e.Guild.GetRole(roleId));
            }
        }
        static async Task HandleMessage(MessageCreateEventArgs e)
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
                        await e.Guild.GetMemberAsync(e.Author.Id).Result.ModifyAsync(e.Guild.GetMemberAsync(e.Author.Id).Result.Nickname.Replace("[AFK] ", ""));
                    }
                    catch { }
                }
                if(e.Author.Id == 366298290377195522 && e.Message.Content.ToLowerInvariant().Equals("am i right lads or am i right?"))
                {
                    await e.Channel.SendMessageAsync("<@!366298290377195522> You are right lad!");
                }
            }
            catch(Exception ee)
            {
                Logger.Log(ee.ToString());
            }
        }
        static async Task Ready(ReadyEventArgs e)
        {
            BotGuild = discord.GetGuildAsync(388339196978266114).Result;
            Mrcarrot = BotGuild.GetMemberAsync(366298290377195522).Result;
            Logger.Log("Connection ready");
            await discord.UpdateStatusAsync(new DiscordGame($"in {discord.Guilds.Count} servers | {commandPrefix}help"));

            if(firstRun)
            {
                await Conversation.Conversation.StartConversation(false);
                firstRun = false;
            }
            LevelingData.LoadDatabase();
            await BotGuild.GetChannel(502841234285527041).SendMessageAsync("CarrotBot ready.");
        }
        static async Task MessageUpdated(MessageUpdateEventArgs e)
        {
            if(Conversation.ConversationData.ConversationMessagesByOrigId.ContainsKey(e.Message.Id))
            {
                await Conversation.ConversationData.ConversationMessagesByOrigId[e.Message.Id].UpdateMessage();
            }
        }
        static async Task MessageDeleted(MessageDeleteEventArgs e)
        {
            if(Conversation.ConversationData.ConversationMessagesByOrigId.ContainsKey(e.Message.Id))
            {
                await Conversation.ConversationData.ConversationMessagesByOrigId[e.Message.Id].DeleteMessage(false);
            }
        }
    }
}
