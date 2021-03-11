using System;
using System.Threading.Tasks;
using System.IO;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;

namespace CarrotBot
{
    class Program
    {
        //Set to true to run beta
        private static readonly bool isBeta = false;
        //Set to true to enable certain debug features such as more verbose logs
        private static readonly bool debug = true;

        public static bool conversation = false;
        static bool firstRun = true;

        public static DiscordClient discord;
        static CommandsNextModule commands;
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
            discord.MessageCreated += HandleMessage;
            discord.Ready += Ready;
            discord.MessageUpdated += MessageUpdated;
            discord.MessageDeleted += MessageDeleted;
            await discord.ConnectAsync();

            BotGuild = discord.GetGuildAsync(388339196978266114).Result;
            Mrcarrot = BotGuild.GetMemberAsync(366298290377195522).Result;
            

            commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefix = commandPrefix
            });
            commands.RegisterCommands<Commands.UngroupedCommands>();
            commands.RegisterCommands<Conversation.ConversationCommands>();
            commands.RegisterCommands<Commands.AdminCommands>();
            commands.RegisterCommands<Commands.BotCommands>();
            commands.RegisterCommands<Commands.MathCommands>();
            commands.RegisterCommands<Commands.UserCommands>();

            await Task.Delay(-1);
        }
        static async Task HandleMessage(MessageCreateEventArgs e)
        {
            if(debug)
                Console.WriteLine($"{e.Author.Username}#{e.Author.Discriminator}: {e.Message.Content}");
            if(conversation && e.Message.Author.Id != discord.CurrentUser.Id)
                await Conversation.Conversation.CarryOutConversation(e.Message);
        }
        static async Task Ready(ReadyEventArgs e)
        {
            Logger.Log("Connection ready");
            await discord.UpdateStatusAsync(new DiscordGame($"in {discord.Guilds.Count} servers | {commandPrefix}help"));

            if(!isBeta && firstRun)
            {
                await Conversation.Conversation.StartConversation();
                firstRun = false;
            }
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
