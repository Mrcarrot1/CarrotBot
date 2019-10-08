using System;
using System.Threading.Tasks;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Net.Http;

namespace CarrotBot
{
    public class Program
    {
        public static bool ControlRoomLink = false;
        public static bool isBeta = false; //Change to run beta
        public static ulong ControlRoomLinkChannel = 0;
        public static bool Link = false;
        public static bool AIMode = false;
        private CommandService commands;
        public static DiscordSocketClient client;
        public static ulong ownerId = 366298290377195522;
        private IServiceProvider services;
        public static SocketUser Mrcarrot;
        string helpCommand = "%help";
        /*public static void Main(string[] args)
        {
            var gui = new GUI();
            gui.ShowDialog();
        }*/

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            Console.Title = "CarrotBot";
            if (isBeta)
                Console.Title += " Beta";
            client = new DiscordSocketClient();
            commands = new CommandService();

            client.Log += Log;

            if (isBeta)
                helpCommand = "b%help";
            services = new ServiceCollection()
                .BuildServiceProvider();

            await InstallCommands();
            if (!isBeta) await client.LoginAsync(TokenType.Bot, SensitiveInformation.botToken);
            else await client.LoginAsync(TokenType.Bot, SensitiveInformation.betaToken);

            client.MessageReceived += MessageReceived;

            client.UserJoined += UserJoined;

            //client.MessageUpdated += MessageUpdated;

            client.MessageDeleted += MessageDeleted;
            //client.MessageReceived += CensorMessage;

            Conversation.LoadDatabase();

            Mrcarrot = client.GetUser(366298290377195522);

            foreach (string id in File.ReadLines($@"{Environment.CurrentDirectory}/TrustedUsers.cb"))
            {
                bool ok = ulong.TryParse(id, out ulong ulongID);
                if (ok)
                    Bot.TrustedUsers.Add(ulongID);
            }

            await client.StartAsync();
            // Block this task until the program is closed.
            await Task.Delay(-1);
            await client.SetStatusAsync(UserStatus.Offline);
        }
        private Task MessageUpdated(Cacheable<IMessage, ulong> cacheable, SocketMessage msg, ISocketMessageChannel channel, Task t)
        {


            return Task.CompletedTask;
        }

        private Task Log(LogMessage msg)
        {
            Logger.Lawg(msg.ToString());
            client.SetGameAsync($"in {client.Guilds.Count} servers | {helpCommand}");
            Mrcarrot = client.GetUser(366298290377195522);
            return Task.CompletedTask;
        }
        private Task UserJoined(SocketGuildUser user)
        {
            var server = (user as IGuildUser).Guild;
            Logger.Lawg($"{DateTime.Now.ToString("HH:mm:ss")} User {user.Username}#{user.Discriminator} joined server {server.Name}(ID {server.Id}).");
            try
            {
                if (user.Username.Contains("discord.gg"))
                    user.KickAsync();
                if (user.Username.ToLower() == "decoyoctopus" && user.Discriminator == "5279")
                    user.KickAsync();
            }
            catch(Exception e)
            {
                Logger.Lawg($"{DateTime.Now.ToString("HH:mm:ss")} {e.ToString()}");
            }
            return Task.CompletedTask;
        }
        private Task MessageDeleted(Cacheable<IMessage, ulong> cacheable, ISocketMessageChannel channel)
        {
            var message = cacheable.Value as SocketMessage;
            var server = (message.Channel as IGuildChannel).Guild;
            Logger.Lawg($"{DateTime.Now.ToString("HH:mm:ss")} Message sent by {message.Author.Username}#{message.Author.Discriminator} deleted in channel {message.Channel.Name} in server {server.Name}. Message Reads: {message.Content}  Message Attachment: {message.Attachments.First().Url}");
            return Task.CompletedTask;
        }
        public static void ThreadProc()
        {
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine("ThreadProc: {0}", i);
                // Yield the rest of the time slice.
                Thread.Sleep(0);
            }
        }
        private async Task MessageReceived(SocketMessage message)
        {
            SocketGuild guild = null;
            if(!message.Channel.GetType().Equals(ChannelType.DM))
                guild = ((SocketGuildChannel)message.Channel).Guild;
            /*if (message.Content == "%ping")
            {
                await message.Channel.SendMessageAsync("Pong!");
            }
            /*if (message.Content.Contains("cabbage"))
                await message.Channel.SendMessageAsync("https://imgur.com/a/BKPNq");
            if (message.Content.Contains("parrot") && message.Author.Username != "CarrotBot")
                await message.Channel.SendMessageAsync("<:thumbsupparrot:483814515138363392:>");*/
            if(!client.GetDMChannelsAsync().Result.ToList().Contains(message.Channel as IDMChannel))
            {
                if ((message.Author.Username != "CarrotBot" && !isBeta) || (message.Author.Username != "CarrotBot Beta" || isBeta))
                {
                    try
                    {
                        if ((message.Channel as IGuildChannel).Guild.GetUsersAsync().Result.Count < 500)
                            Logger.Lawg($"{DateTime.Now.ToString("HH:mm:ss")} Received Message in #" + message.Channel.Name + $"(Id {message.Channel.Id}) in server {guild.Name}(Id {guild.Id}) from user " + message.Author.Username + ". Message reads: " + message.Content/* + $" and has attachments: {message.Attachments.ToString()}"*/);
                        File.AppendAllText($@"{Environment.CurrentDirectory}/Logs/guild_{(message.Channel as IGuildChannel).GuildId}.log", $@"{DateTime.Now.ToString("DD_MM_HH:MM:SS")}    {message.Author.Username}#{message.Author.Discriminator}({message.Author.Id}): {message.Content}");
                    }
                    catch
                    {
                        Logger.Lawg($"Received Direct Message from {message.Author.Username}${message.Author.Discriminator}. Message Reads: {message.Content}");
                    }
                }
                else
                {
                    if (message.Channel.Id != 490551836323872779)
                        Logger.Lawg($"{DateTime.Now.ToString("HH:mm:ss")} Sent Message in #" + message.Channel.Name + $" in server {guild.Name}(Id {guild.Id}). Message reads: " + message.Content);
                }
            }
            else
            {
                if (message.Author.Username != "CarrotBot")
                {
                    Logger.Lawg($"{DateTime.Now.ToString("HH:mm:ss")} Received Direct Message from {message.Author.Username}#{message.Author.Discriminator}. Message reads: " + message.Content/* + $" and has attachments: {message.Attachments.ToString()}"*/);
                }
                else
                {
                    if (message.Channel.Id != 490551836323872779)
                        Logger.Lawg($"{DateTime.Now.ToString("HH:mm:ss")} Sent Direct Message to user {(message.Channel as IDMChannel).Recipient.Username}#{(message.Channel as IDMChannel).Recipient.Discriminator}. Message reads: " + message.Content);
                }
            }
            if (message.Content == "Am I right lads or am I right?" && message.Author.Id == 366298290377195522)
            {
                await message.Channel.SendMessageAsync($"<@{message.Author.Id}> You are right lad!");
            }
            if (message.Content == "1+1")
            {
                await message.Channel.SendMessageAsync($"1 + 1 = Carrot. It's a well-known(okay, maybe not :P) fact.");
            }
            if(message.Content == "C# or Java?")
            {
                await message.Channel.SendMessageAsync($@"C#, my literal lifeblood\*, is far superior to that pathetic Java.\n\*I'm a bot programmed in C#.");
            }
            if(message.Content == "Lua")
            {
                await message.Channel.SendMessageAsync("No! Lua is as bad as that pathetic and demented Java. Use C# instead. For everything. Ever.");
            }
            if(AIMode)
                await Messages(message.Channel);
            int argPos = 0;
            if(message.Author.Id != 389513870835974146 && Link && !message.Content.StartsWith("-ignore"))
            {
                Thread.Sleep(1);
                Conversation.CarryOutConversation(message as IUserMessage);
            }
            if (message.Author.Id != 389513870835974146 && ControlRoomLink && !message.Content.StartsWith("-ignore") && message.Channel.Id == ControlRoomLinkChannel)
            {
                Thread.Sleep(1);
                await (client.GetChannel(502841234285527041) as SocketTextChannel).SendMessageAsync($"{message.Author.Username}#{message.Author.Discriminator}: {message.Content}");
            }
            if (message.Author.Id != 389513870835974146 && ControlRoomLink && !(message.Content.StartsWith("-ignore") || message.Content.StartsWith("%channel linktocontrolroom")) && message.Channel.Id == 502841234285527041)
            {
                Thread.Sleep(1);
                await (client.GetChannel(ControlRoomLinkChannel) as SocketTextChannel).SendMessageAsync($"{message.Content}");
            }
            if (message.Content.Contains("<@389513870835974146>") || message.Content.Contains("<@!389513870835974146>"))
            {
                var pingSock = client.GetGuild(388339196978266114).Emotes.FirstOrDefault(e => e.Id == 494289252788600833);
                await (message as IUserMessage).AddReactionAsync(pingSock);
            }
            if(message.Channel.Id == 495698925513211906)
            {
                SocketUser Mrcarrot = client.GetUser(366298290377195522);
                await Mrcarrot.SendMessageAsync($"KSS Main dev update: {message.Author.Username}: {message.Content}");
                Thread.Sleep(2);
                if (message.Attachments.Count > 0)
                    await Mrcarrot.SendMessageAsync($"  Attachment: {message.Attachments.First().Url}");
            }
        }
        public async Task InstallCommands()
        {
            // Hook the MessageReceived Event into our Command Handler
            client.MessageReceived += HandleCommand;
            // Discover all of the commands in this assembly and load them.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }
        public async Task HandleCommand(SocketMessage messageParam)
        {
            await HandleCommandAsync(messageParam);
        }
        public async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            bool messageIsCommand = message.HasCharPrefix('%', ref argPos);
            if (isBeta)
            {
                argPos = 0;
                messageIsCommand = message.HasStringPrefix("b%", ref argPos);
            }
            if (!messageIsCommand)
            {
                argPos = 0;
                messageIsCommand = message.HasStringPrefix("cb%", ref argPos);
            }
            if (!messageIsCommand) return;
            // Create a Command Context
            var context = new CommandContext(client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            /*var typing = message.Channel.EnterTypingState();
            Thread.Sleep(new Random().Next(25, 2000));
            typing.Dispose();*/
            Logger.Lawg($"Command being executed by user {message.Author.Username}#{message.Author.Discriminator}. Message reads: {message.Content}");
            var result = await commands.ExecuteAsync(context, argPos, services);
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }
        /*public async Task CensorMessage(SocketMessage msg)
        {
            string[] bannedWords =
            {
                "fuck",
                "shit",
                "ass ",
                "bitch",
                "whore",
            };
            foreach(string str in bannedWords)
            {
                if(msg.Content.Contains(str))
                {
                    await msg.DeleteAsync();
                    await msg.Author.SendMessageAsync("Please watch your language!");
                }
            }
        }*/
        public async Task Messages(ISocketMessageChannel channel)
        {
            await channel.SendMessageAsync(Console.ReadLine());
        }
        public static SocketUser GetUser(ulong id)
        {
            return client.GetUser(id);
        }
    }
}