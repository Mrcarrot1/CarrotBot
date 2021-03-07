using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using NCalc2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using ZipFile = System.IO.Compression.ZipFile;
using Google;
using Google.Apis;
using Google.Apis.Requests;
using Google.Apis.Customsearch.v1;
using Google.Apis.Services;
using Google.Apis.Customsearch.v1.Data;

namespace CarrotBot
{
    [Name("prefixlesscommands")]
    public class PrefixlessCommands : ModuleBase
    {
        [Command("minecon")]
        public async Task Minecon()
        {
            await Context.Channel.SendMessageAsync("https://www.minecraft.net/en-us/article/how-watch-minecon-live");
        }
        [Command("arrest")]
        public async Task ArrestUser(string user, [Remainder]string crimes)
        {
            ulong userId = CommandUtils.GetID(user);
            string[] ArrestMessages =
            {
                $"*Police sirens in distance*\n<@{userId}>, you have the right to remain silent. You watch TV. You know the rest. You stand accused of {crimes}.",
                $"*Robot cop busts down door and runs in with a gun*\n<@{userId}>, You have the right to remain silent! SO SHUT UP!\nYou have been accused of {crimes}!"
            };
            await Context.Channel.SendMessageAsync(ArrestMessages[new Random().Next(0, 2)]);
            //Thread.Sleep(3);
            //await Context.Channel.SendMessageAsync($"-mute <@{userId}>");
        }
        [Command("help")]
        public async Task Help()
        {
            var eb = new EmbedBuilder
            {
                Color = Color.Green
            };

            eb.Description = $"{File.ReadAllText($@"{Environment.CurrentDirectory}/Commands.cb")}";
            eb.WithTitle("Commands");
            await Context.User.SendMessageAsync("", false, eb);
            eb.Description = $"{File.ReadAllText($@"{Environment.CurrentDirectory}/Commands2.cb")}";
            eb.WithTitle("");
            eb.WithFooter("© Mrcarrot 2018-19. All Rights Reserved.");
            await Context.User.SendMessageAsync("", false, eb);
            await Context.Channel.SendMessageAsync("Check your DMs!");
        }
        [Command("updateping")]
        public async Task UpdatePing()
        {
            if (Context.Guild.Id != 388339196978266114)
            {
                await Context.Channel.SendMessageAsync("Unknown command.");
                return;
            }
            IGuildUser user = Context.Guild.GetUserAsync(Context.User.Id).Result;
            IRole role = Context.Guild.GetRole(557698296211046433);
            await user.AddRoleAsync(role);
        }
        /*[Command("type")]
        public async Task Type(int time)
        {
            RequestOptions requestOptions = new RequestOptions();
            requestOptions.Timeout = time;
            await Context.Channel.TriggerTypingAsync(requestOptions);
        }*/
        [Command("clear")]
        public async Task Clear(int numberMsgs)
        {
            if (!Context.Guild.GetUserAsync(Context.User.Id).Result.GuildPermissions.ManageMessages && Context.User.Id != Program.Mrcarrot.Id)
            {
                await Context.Channel.SendMessageAsync("You don't have permission to do that!");
                return;
            }
            var messages = Context.Channel.GetMessagesAsync(numberMsgs + 1).ToList().Result;
            foreach (IReadOnlyCollection<IMessage> msgs in messages)
            {
                foreach (IMessage msg in msgs)
                {
                    await msg.DeleteAsync();
                    Thread.Sleep(1);
                }
            }
        }
        [Command("crole")]
        public async Task CRole(string name, int r = 0, int g = 0, int b = 0)
        {
            Context.Guild.CreateRoleAsync(name, color: new Color(r, g, b));
            Thread.Sleep(75);
            IRole role = null;
            while (role == null)
            {
                role = Context.Guild.Roles.FirstOrDefault(x => x.Name == name);
                Thread.Sleep(5);
            }
            Context.Guild.GetUserAsync(Context.User.Id).Result.AddRoleAsync(role);
        }
    }
    [Group("math")]
    public class Sample : ModuleBase
    {
        [Command("square"), Summary("Squares a number.")]
        public async Task Square([Summary("The number to square.")] int num)
        {
            await Context.Channel.SendMessageAsync($"{num}² = {Math.Pow(num, 2)}");
        }

        [Command("powerof"), Summary("Takes a factor and puts it to the pwer of a given exponent.")]
        public async Task Power([Summary("The number to use as a factor.")] int num, [Summary("The exponent.")] int power)
        {
            await Context.Channel.SendMessageAsync($"{num}^{power} = {Math.Pow(num, power)}");
        }
        [Command("add"), Summary("Adds two numbers.")]
        public async Task Add([Summary("The first number to add.")] double num1, [Summary("The number to add to it.")] double num2)
        {
            await Context.Channel.SendMessageAsync($"{num1} + {num2} = {num1 + num2}");
        }
        [Command("subtract"), Summary("Subtracts a number from another number")]
        public async Task Subtract([Summary("The number to subtract from.")] double num1, [Summary("The number to subtract.")] double num2)
        {
            await Context.Channel.SendMessageAsync($"{num1} - {num2} = {num1 - num2}");
        }
        [Command("multiply"), Summary("Multiplies two numbers.")]
        public async Task Multiply([Summary("The first factor.")] double num1, [Summary("The second factor.")] double num2)
        {
            await Context.Channel.SendMessageAsync($"{num1} * {num2} = {num1 * num2}");
        }
        [Command("divide"), Summary("Divides one number by another.")]
        public async Task Divide([Summary("The dividend.")] double num1, [Summary("The divisor.")] double num2)
        {
            await Context.Channel.SendMessageAsync($"{num1} / {num2} = {num1 / num2}");
        }
        [Command("sqrt"), Summary("Finds the square root of a number.")]
        public async Task Sqrt([Summary("The number to find the square root of.")] double num)
        {
            await Context.Channel.SendMessageAsync($"sqrt({num}) = {Math.Sqrt(num)}");
        }
        //Kept unused until I can find a way to make it work right
        /*[Command("average"), Summary("Returns the average of a specified number of numbers.")]
        public async Task Average(int count, double num1, double num2, double num3 = 0, double num4 = 0, double num5 = 0, double num6 = 0, double num7 = 0, double num8 = 0, double num9 = 0, double num10 = 0)
        {
            List<int> theAveragers = new List<int>(); //Featuring Iron Mean
            for(int i = 1; i <= count; i++)
            {

            }
        }*/
        [Command("round")]
        public async Task Round(double value)
        {
            await Context.Channel.SendMessageAsync($"{Math.Round(value)}");
        }
        [Command("cos")]
        public async Task Cos(double value)
        {
            await Context.Channel.SendMessageAsync($"{Math.Cos(value)}");
        }
        [Command("cosh")]
        public async Task Cosh(double value)
        {
            await Context.Channel.SendMessageAsync($"{Math.Cosh(value)}");
        }
        [Command("eval")]
        public async Task Eval(string expression)
        {
            Debug.Assert("kep has been pinged" == new Expression("<@245703456382386177>").Evaluate());
            Debug.Assert("kep has been pinged" == new Expression("<@!245703456382386177>").Evaluate());
            Expression e = new Expression(expression, EvaluateOptions.IgnoreCase);
            await Context.Channel.SendMessageAsync(e.Evaluate().ToString());
        }
    }
    [Group("role")]
    public class Role : ModuleBase
    {
        public static List<ulong> AssignableRoles = new List<ulong>();
        [Command("add")]
        public async Task Add(string roleName, ulong userId = 0)
        {
            if ((Context.User as IGuildUser).GuildPermissions.ManageRoles || Context.User.Id == 366298290377195522)
            {
                IRole role = Context.Guild.Roles.FirstOrDefault(r => r.Name == roleName);
                if (role != null)
                {
                    if (userId == 0)
                    {
                        await (Context.User as IGuildUser).AddRoleAsync(role);
                    }
                    else
                    {
                        IGuildUser user = Program.GetUser(userId) as IGuildUser;
                        await user.AddRoleAsync(role);
                    }
                }
            }
        }
        [Command("get")]
        public async Task Get(string roleName)
        {
            AssignableRoles = new List<ulong>();
            foreach (string str in File.ReadAllLines($@"{Environment.CurrentDirectory}/AssignableRoles.cb"))
            {
                if (ulong.TryParse(str, out ulong Id))
                {
                    AssignableRoles.Add(Id);
                }

            }
            IRole role = Context.Guild.Roles.FirstOrDefault(r => r.Name == roleName);
            if (role == null) return;
            bool Assignable = false;
            foreach (ulong Id in AssignableRoles)
            {
                if (Id == role.Id)
                    Assignable = true;
            }

            if (Assignable)
            {
                await (Context.User as IGuildUser).AddRoleAsync(role);
            }
            else
            {
                await Context.Channel.SendMessageAsync("That role is not assignable!");
                return;
            }

        }
        [Command("assignable")]
        public async Task Assignable()
        {
            string roles = "";
            for (int i = 0; i < AssignableRoles.Count; i++)
            {
                roles += $"{AssignableRoles[i]}\n";
            }
            await Context.User.SendMessageAsync(roles);
        }
    }
    [Group("bot")]
    public class Bot : ModuleBase
    {
        public static List<ulong> TrustedUsers = new List<ulong>();
        [Command("info"), Summary("Returns info about the bot.")]
        public async Task Info()
        {
            var eb = new EmbedBuilder();
            eb.WithDescription($"CarrotBot is a bot made by Mrcarrot#3305.\nThe bot is in {Program.client.Guilds.Count} servers.");
            eb.Color = Color.Green;
            eb.WithFooter("© Mrcarrot 2018-19. All Rights Reserved.");
            await Context.Channel.SendMessageAsync("", false, eb);
        }
        [Command("installlib")]
        public async Task InstallLib()
        {
            if (Context.User.Id != 366298290377195522) return;
            IAttachment[] attachments = Context.Message.Attachments.ToArray();
            if (attachments.Length == 0)
            {
                await Context.Channel.SendMessageAsync("Attach a file!");
                return;
            }
            if (!attachments[0].Url.Contains(".zip"))
            {
                await Context.Channel.SendMessageAsync("Only .zip files are supported!");
                return;
            }
            WebClient client = new WebClient();
            client.DownloadFile(new Uri(attachments[0].Url), $@"{Environment.CurrentDirectory}/lib.zip");
            ZipFile.ExtractToDirectory($@"{Environment.CurrentDirectory}/lib.zip", Environment.CurrentDirectory);
        }
        [Command("setnick")]
        public async Task SetNick(string nick)
        {
            var bot = Context.Guild.GetUserAsync(389513870835974146).Result;
            if (Context.User.Id == 366298290377195522)
            {
                await bot.ModifyAsync(x =>
                {
                    x.Nickname = nick;
                });
                await Context.Channel.SendMessageAsync($"Set bot nickname to {nick}.");
            }
            else
            {
                await Context.Channel.SendMessageAsync("You don't have permission to do that!");
            }
        }
        [Command("addtrusteduser")]
        public async Task AddTrustedUser([Summary("The ID of the user to add.")] ulong id)
        {
            if (Context.User.Id == 366298290377195522)
            {
                if (!TrustedUsers.Contains(id))
                {
                    TrustedUsers.Add(id);
                    File.AppendAllText($@"{Environment.CurrentDirectory}/TrustedUsers.cb", $"\n{id}");
                    Logger.Lawg($"{DateTime.Now.ToString("HH:mm:ss")} Added Trusted User: {id}");
                    await Context.Channel.SendMessageAsync($"Added trusted user: <@{id}>");
                }
                else
                {
                    Logger.Lawg($"{DateTime.Now.ToString("HH:mm:ss")} Error. Tried to add already trusted user to trusted list.");
                    await Context.Channel.SendMessageAsync("That user is already trusted!");
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Looks like you don't have permission to do that, <@{Context.User.Id}>");
            }
        }
        [Command("removetrusteduser")]
        public async Task RemoveTrustedUser([Summary("The ID of the user to remove.")] ulong id)
        {
            if (Context.User.Id == 366298290377195522)
            {
                if (TrustedUsers.Contains(id))
                {
                    TrustedUsers.Remove(id);
                    File.WriteAllText($@"{Environment.CurrentDirectory}/TrustedUsers.cb", (File.ReadAllText($@"{Environment.CurrentDirectory}/TrustedUsers.cb").Replace(id.ToString(), "")));
                    Logger.Lawg($"{DateTime.Now.ToString("HH:mm:ss")} Removed Trusted User: {id}");
                    await Context.Channel.SendMessageAsync($"Removed trusted user: <@{id}>");
                }
                else
                {
                    Logger.Lawg($"{DateTime.Now.ToString("HH:mm:ss")} Error. Tried to remove user not on trusted list from trusted list.");
                    await Context.Channel.SendMessageAsync("That user is not trusted!");
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Looks like you don't have permission to do that, <@{Context.User.Id}>");
            }
        }
        [Command("getlatestlog")]
        public async Task UploadLog()
        {
            if (TrustedUsers.Contains(Context.User.Id))
            {
                await Context.Channel.SendFileAsync(Logger.GetLatestLogZipped());
                Logger.firstRun = true;
            }
            else
            {
                await Context.Channel.SendMessageAsync("You don't have permission to do that!");
            }
        }
        [Command("getguildlog")]
        public async Task UploadGuildLog(ulong guildId)
        {
            if (TrustedUsers.Contains(Context.User.Id))
            {
                await Context.Channel.SendFileAsync(Logger.GetGuildLogZipped(guildId));
            }
            else
                await Context.Channel.SendMessageAsync("You don't have permission to do that!");
        }
        [Command("getalllogs")]
        public async Task UploadAllLogs()
        {
            if (TrustedUsers.Contains(Context.User.Id))
            {
                await Context.Channel.SendFileAsync(Logger.GetAllLogsZipped());
            }
            else
                await Context.Channel.SendMessageAsync("You don't have permission to do that!");
        }
        [Command("update")]
        public async Task UpdateBot()
        {
            if (Context.User.Id != 366298290377195522)
                return;
            if (Context.Message.Attachments.ToArray().Length == 0)
            {
                await Context.Channel.SendMessageAsync("Upload a file!");
            }
            else
            {
                await Context.Channel.SendMessageAsync("Updating CarrotBot...");
                Updater.UpdateBot(Context.Message.Attachments.First().Url);
            }
        }
        [Command("suggestfeature")]
        public async Task SuggestFeature(string feature)
        {
            File.AppendAllText($@"{Environment.CurrentDirectory}/Suggestions.cb", $"{feature}\n");
            await Context.Channel.SendMessageAsync($"Suggestion Added: {feature}.");
            await Program.client.GetUser(366298290377195522).SendMessageAsync($"Feature suggestion made by {Context.User.Username + "#" + Context.User.Discriminator} in server {Context.Guild.Name}: {feature}.");
        }
        [Command("reportbug")]
        public async Task ReportBug(string bug)
        {
            await Program.GetUser(366298290377195522).SendMessageAsync($"Bug reported by {Context.User.Username}#{Context.User.Discriminator}:\n{bug}");
            File.AppendAllText($@"{Environment.CurrentDirectory}/Bugs.cb", $"{bug}\n");
            await Context.Channel.SendMessageAsync($"Bug Reported: {bug}.");
        }
        [Command("poweroff")]
        public async Task PowerOff()
        {
            if (Context.User.Id == 366298290377195522)
            {
                await Context.Channel.SendMessageAsync("CarrotBot powering off. Good Night...");
                await Program.client.SetStatusAsync(UserStatus.Offline);
                if (Program.Link)
                {
                    foreach (ConversationChannel channel in Conversation.channels)
                    {
                        await (Program.client.GetChannel(channel.Id) as ISocketMessageChannel).SendMessageAsync("The CarrotBot Multi-Server Conversation is no longer active.");
                        Thread.Sleep(5);
                    }
                }
                Environment.Exit(0);
            }
        }
        [Command("restart")]
        public async Task Restart()
        {
            if (Context.User.Id == 366298290377195522)
            {
                await Context.Channel.SendMessageAsync("CarrotBot restarting. Give me a moment...");
                await Program.client.SetStatusAsync(UserStatus.Offline);
                if (Program.Link)
                {
                    foreach (ConversationChannel channel in Conversation.channels)
                    {
                        await (Program.client.GetChannel(channel.Id) as ISocketMessageChannel).SendMessageAsync("The CarrotBot Multi-Server Conversation is no longer active.");
                        Thread.Sleep(1);
                    }
                }
                Process.Start($@"{Environment.CurrentDirectory}/CarrotBot.exe");
                Environment.Exit(0);
            }
        }
        [Command("invite")]
        public async Task Invite()
        {
            await Context.Channel.SendMessageAsync("https://discordapp.com/oauth2/authorize?client_id=389513870835974146&scope=bot&permissions=8");
        }
        [Command("aimode")]
        public async Task AIMode()
        {
            if (Context.User.Id == 366298290377195522)
            {
                if (Program.AIMode)
                    Program.AIMode = false;
                else
                    Program.AIMode = true;
            }
            else
            {
                await Context.Channel.SendMessageAsync("Unknown command!");
            }
        }
        [Command("server")]
        public async Task Server()
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.Color = Color.Green;
            eb.WithDescription("https://discord.gg/wHPwHu7");
            await Context.Channel.SendMessageAsync("Go to the server for CarrotBot support and testing by joining:", false, eb);
        }
        [Command("permissions")]
        public async Task Permissions(ulong guildId = 0)
        {
            var guild = Program.client.GetGuild(guildId);
            if (guildId == 0)
                guild = Context.Guild as SocketGuild;
            var user = guild.GetUser(389513870835974146);
            string permissions = "";
            foreach (GuildPermission permission in user.GuildPermissions.ToList())
            {
                permissions += $"{permission.ToString()}\n";
            }
            var eb = new EmbedBuilder();
            eb.WithFooter("© Mrcarrot 2018-19. All Rights Reserved.");
            eb.Color = Color.Green;
            eb.WithDescription(permissions);
            eb.WithTitle($"**Bot Permissions in Server {guild.Name}**");
            await Context.Channel.SendMessageAsync("", false, eb);
        }
    }
    [Group("server")]
    public class Server : ModuleBase
    {
        [Command("owner")]
        public async Task Owner()
        {
            var eb = new EmbedBuilder();
            eb.WithDescription($"<@{Context.Guild.OwnerId}>\n{Program.GetUser(Context.Guild.OwnerId).Username + "#" + Program.GetUser(Context.Guild.OwnerId).Discriminator}\n{Context.Guild.OwnerId}\n{Context.Guild.Name}");
            eb.Color = Color.Green;
            eb.WithFooter("© Mrcarrot 2018-19. All Rights Reserved.");
            eb.WithThumbnailUrl(Program.client.GetUser(Context.Guild.OwnerId).GetAvatarUrl());
            eb.WithTitle("Server Owner");
            await Context.Channel.SendMessageAsync("", false, eb);
        }
        [Command("info")]
        public async Task Info()
        {
            var eb = new EmbedBuilder();
            eb.Color = Color.Green;
            eb.WithFooter("© Mrcarrot 2018-19. All Rights Reserved.");
            eb.WithThumbnailUrl(Context.Guild.IconUrl);
            eb.WithTitle("Server Info");
            eb.WithDescription($"Name: {Context.Guild.Name}\nOwner: <@{Context.Guild.OwnerId}>\nVoice Region: {Context.Guild.VoiceRegionId}\nCreated at: {Context.Guild.CreatedAt.ToString()}\nText Channels: {Context.Guild.GetTextChannelsAsync().Result.ToList().Count}\nVoice Channels: {Context.Guild.GetVoiceChannelsAsync().Result.ToList().Count}");
            await Context.Channel.SendMessageAsync("", false, eb);
        }
        [Command("channels")]
        public async Task Channels(ulong guildId = 0)
        {
            var guild = Program.client.GetGuild(guildId);
            if (guildId == 0)
                guild = Context.Guild as SocketGuild;
            string[] channels = { "", "", "", "", "", "", "", "", "", "", "", "", "" };
            int i = 0;
            foreach (SocketGuildChannel channel in guild.Channels.ToList())
            {
                if (channel.Name != null)
                {
                    if (channels[i].Length + $"<#{channel.Id}>({channel.Id})\n".Length > 1024) i++;
                    channels[i] += $"<#{channel.Id}>({channel.Id})\n";
                }

            }
            var eb = new EmbedBuilder();
            for (i = 0; i < channels.Length; i++)
            {

                eb.Color = Color.Green;

                eb.WithFooter("© Mrcarrot 2018-19. All Rights Reserved.");
                var field = new EmbedFieldBuilder();
                field.Name = $"List Part {i}";
                field.Value = channels[i];
                eb.AddField(field);
                eb.WithTitle($"**Channels in Server {guild.Name}**");
                if (channels[i + 1] == "") break;
            }
            await Context.Channel.SendMessageAsync("", false, eb);
            Thread.Sleep(1);

        }
        [Command("invite")]
        public async Task GetServerInvite(ulong guildId = 0)
        {
            var guild = Program.client.GetGuild(guildId);
            if (guildId == 0)
                guild = Context.Guild as SocketGuild;
            string invite = guild.GetInvitesAsync().Result.First().ToString();
            if (Bot.TrustedUsers.Contains(Context.User.Id))
                await Context.Channel.SendMessageAsync($"{invite}");
            else
                await Context.Channel.SendMessageAsync("You don't have permission to do that!");
        }
    }
    [Group("channel")]
    public class Channel : ModuleBase
    {
        [Command("getid")]
        public async Task GetID(string channel)
        {
            string channelIDString = channel
                .Replace("<", "")
                .Replace("#", "")
                .Replace(">", "");
            if (ulong.TryParse(channelIDString, out ulong channelID))
            {
                await Context.Channel.SendMessageAsync(channelID.ToString());
            }
        }
        [Command("linktocontrolroom")]
        public async Task ControlRoomLink(ulong channelId = 0)
        {
            if (Context.User.Id == 366298290377195522)
            {
                if (Program.ControlRoomLink)
                {
                    Program.ControlRoomLink = false;
                }
                else if (channelId != 0)
                {
                    Program.ControlRoomLink = true;
                    Program.ControlRoomLinkChannel = channelId;
                }
                else if (channelId == 0)
                {
                    await Context.Channel.SendMessageAsync("Please specify a channel to link to!");
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync("Unknown command.");
            }
        }
        [Command("rename")]
        public async Task Rename(string channelHashTag, string name)
        {
            ulong channelId = CommandUtils.GetID(channelHashTag);
            var channel = Program.client.GetChannel(channelId) as SocketTextChannel;
            string oldChannelName = channel.Name;
            await channel.ModifyAsync(c => c.Name = name);
            await Context.Channel.SendMessageAsync($"Set new name of #{oldChannelName} to #{name}");
        }
        [Command("get")]
        public async Task GetChannel(ulong id)
        {
            await Context.Channel.SendMessageAsync($"{Program.client.GetChannel(id)}");
        }
        [Command("link")]
        public async Task ChannelLink(bool silent = false)
        {
            if (Context.User.Id == 366298290377195522)
            {
                if (Program.Link)
                {
                    Conversation.LoadDatabase();
                    Program.Link = false;
                    foreach (ConversationChannel channel in Conversation.channels)
                    {
                        if (!silent)
                        {
                            Conversation.SendConversationMessage("The CarrotBot Multi-Server Conversation is no longer active.");
                        }
                    }
                    await Context.Channel.SendMessageAsync("Deactivated multi-server conversation.");
                }
                else
                {
                    Program.Link = true;
                    if (!silent)
                    {
                        Conversation.LoadDatabase();
                        foreach (ConversationChannel channel in Conversation.channels)
                        {
                            await (Program.client.GetChannel(channel.Id) as ISocketMessageChannel).SendMessageAsync("The CarrotBot Multi-Server Conversation is active! Remember: you need to accept the terms(`%terms accept`) to chat here!");
                            Thread.Sleep(5);
                        }
                    }
                    await Context.Channel.SendMessageAsync("Activated multi-server conversation.");
                }
            }
        }
        [Command("addtolink")]
        public async Task AddLinkChannel(ulong channelID, string channelCallSign)
        {
            if (Bot.TrustedUsers.Contains(Context.User.Id))
            {
                File.AppendAllText($@"{Environment.CurrentDirectory}/ConversationServers.csv", $"\n{channelID},{channelCallSign}");
                await Context.Channel.SendMessageAsync("Added conversation channel.");
            }
            else
            {
                await Program.Mrcarrot.SendMessageAsync($"Conversation channel requested: Id {channelID}, Call sign \"{channelCallSign}\"");
                await Context.Channel.SendMessageAsync("Your request has been submitted. Please be patient, real humans have to approve it.");
            }
        }
        [Command("removefromlink")]
        public async Task RemoveLinkChannel(ulong Id)
        {
            if (Context.User.Id != 366298290377195522) return; //This command is much more limiting than the other

            foreach (string line in File.ReadAllLines($@"{Environment.CurrentDirectory}/ConversationServers.csv"))
            {
                if (line.StartsWith(Id.ToString()))
                    File.WriteAllText($@"{Environment.CurrentDirectory}/ConversationServers.csv", File.ReadAllText($@"{Environment.CurrentDirectory}/ConversationServers.csv").Replace(line, ""));
            }
        }
    }
    [Group("terms")]
    public class Terms : ModuleBase
    {
        [Command("accept")]
        public async Task Accept(bool accept = false)
        {
            if (accept)
            {
                await Context.Channel.SendMessageAsync($"<@{Context.User.Id}>, you have agreed to having your data read and used by others.");
                Conversation.AcceptedUsers.Add(Context.User.Id);
                File.AppendAllText($@"{Environment.CurrentDirectory}/AcceptedUsers.cb", $"{Context.User.Id},");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"<@{Context.User.Id}>, you are about to agree to having your data read and used by others. Type `%terms accept true` to accept.");
            }
        }
    }
    [Group("user")]
    public class User : ModuleBase
    {
        [Command("info")]
        public async Task Info(string userPing = null)
        {
            ulong userId = 0;
            if (userPing != null)
                userId = CommandUtils.GetID(userPing);
            var user = Context.User as SocketUser;
            if (userId != 0)
                user = Program.client.GetUser(userId);
            string type = "User";
            if (user.IsBot)
                type = "Bot";
            if (user.IsWebhook)
                type = "Webhook";
            if (user.Id == 366298290377195522)
                type = "**Robot Overlord**";
            if (user.Id == Program.client.CurrentUser.Id)
                type = "Yours truly";
            var eb = new EmbedBuilder();
            if (user.Status.ToString() == "Online")
                eb.Color = Color.Green;
            if (user.Status.ToString() == "Idle")
                eb.Color = Color.Gold;
            if (user.Status.ToString() == "DoNotDisturb")
                eb.Color = new Color(255, 0, 0);
            if (user.Status.ToString() == "Offline")
                eb.Color = Color.DarkGrey;
            string status = user.Status.ToString();
            if (status == "DoNotDisturb")
                status = "Busy";
            IGuildUser guildUser = Context.Guild.GetUserAsync(user.Id).Result;
            string nick = guildUser.Nickname;
            eb.WithFooter("© Mrcarrot 2018-19. All Rights Reserved.");
            eb.WithThumbnailUrl(user.GetAvatarUrl());
            eb.WithTitle("User Info");
            eb.WithDescription($"{user.Username + "#" + user.Discriminator}\nNickname: {nick}\nStatus: {status}\nCreated At: {user.CreatedAt} (UTC)\nPlaying: {user.Game}\nType: {type}");
            await Context.Channel.SendMessageAsync("", false, eb);
        }
        [Command("get")]
        public async Task Get(string userPing)
        {
            ulong userId = CommandUtils.GetID(userPing);
            var user = Program.client.GetUser(userId);
            await Context.Channel.SendMessageAsync($"{user.Username}#{user.Discriminator}");
        }
        [Command("permissions")]
        public async Task Permissions(ulong userId)
        {
            string permissions = "";
            foreach (GuildPermission permission in Context.Guild.GetUserAsync(userId).Result.GuildPermissions.ToList())
            {
                permissions += $"{permission.ToString()}\n";
            }
            var eb = new EmbedBuilder();
            eb.WithDescription(permissions);
            eb.WithTitle($"**Permissions of {Program.GetUser(userId).Username}**");
            eb.WithFooter("© Mrcarrot 2018-19. All Rights Reserved.");
            eb.Color = Color.Green;
            await Context.Channel.SendMessageAsync("", false, eb);
        }
        [Command("getid")]
        public async Task GetId(string userPing)
        {
            string userIdStr = userPing
                .Replace("<", "")
                .Replace("$@", "")
                .Replace("!", "")
                .Replace(">", "");
            bool ok = ulong.TryParse(userIdStr, out ulong userId);
            if (ok)
            {
                try
                {
                    var user = Program.client.GetUser(userId);
                    await Context.Channel.SendMessageAsync(user.Id.ToString());
                }
                catch (Exception e)
                {
                    Logger.Lawg($"{DateTime.Now.ToString("HH:mm:ss")} {e.ToString()}");
                    await Context.Channel.SendMessageAsync(CommandUtils.CensorFilepath(e.ToString()));
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync("Could not obtain user ID from ping!");
            }
        }
    }
    [Group("random")]
    public class RandomStuff : ModuleBase
    {
        [Command("string")]
        public async Task String(int length)
        {
            string RndString = "";
            if (length > 1994)
            {
                await Context.Channel.SendMessageAsync("Max length is 1994!");
                return;
            }
            RNGCryptoServiceProvider RNG = new RNGCryptoServiceProvider();
            byte[] data = new byte[2];
            for (int i = 0; i < length; i++)
            {
                RNG.GetBytes(data);
                RndString += BitConverter.ToChar(data, 0);
            }
            await Context.Channel.SendMessageAsync($"```{RndString}```");
        }
        [Command("int32")]
        public async Task Int(int min, int max)
        {
            if (min > max)
            {
                await Context.Channel.SendMessageAsync("Min must not be greater than max!");
                return;
            }
            Random rnd = new Random();
            RandomNumberGenerator cryptoRnd = RandomNumberGenerator.Create();
            int output = rnd.Next(min, (max + 1));
            byte[] data =
            {
                0
            };

            while (data[0] < min || data[0] > max)
                cryptoRnd.GetBytes(data);
            await Context.Channel.SendMessageAsync($"{output}");
        }
        [Command("int64")]
        public async Task Int64()
        {
            RNGCryptoServiceProvider RNG = new RNGCryptoServiceProvider();
            byte[] data = new byte[8];
            RNG.GetBytes(data);
            await Context.Channel.SendMessageAsync($"{BitConverter.ToInt64(data, 0)}");
        }
        [Command("cryptobyte")]
        public async Task CryptoRandom()
        {
            RandomNumberGenerator cryptoRnd = RandomNumberGenerator.Create();
            byte[] data =
            {
                0
            };
            cryptoRnd.GetBytes(data);
            await Context.Channel.SendMessageAsync($"{data[0]}");
        }
    }
    public class CommandUtils
    {
        public static ulong GetID(string source)
        {
            ulong Id;
            string IdStr = source
                .Replace("<", "")
                .Replace("@", "")
                .Replace("!", "")
                .Replace("#", "")
                .Replace("&", "")
                .Replace(">", ""); //A user mention is <@Id>, channels are <#Id>, etc. so we're removing those extra chars
            bool ok = ulong.TryParse(IdStr, out Id);
            if (ok)
                return Id;
            else
            {
                throw new FormatException(); //The string wasn't formatted correctly if the ID was invalid
                return 0;
            }
        }
        public static string CensorFilepath(string Exception)
        {
            string censoredPath = Exception.Replace("", "");
            return censoredPath;
        }
    }
    [Group("reminder")]
    public class Reminders : ModuleBase
    {
        [Command("add")]
        public async Task Add(string reminder)
        {

        }
    }
    [Group("message")]
    public class Message : ModuleBase
    {
        [Command("say")]
        public async Task SayInChannel(string message, ulong channelId = 0, bool tts = false)
        {
            if (Bot.TrustedUsers.Contains(Context.User.Id))
            {
                ISocketMessageChannel channel;
                if (channelId == 0)
                {
                    channel = Context.Channel as ISocketMessageChannel;
                }
                else
                {
                    channel = Program.client.GetChannel(channelId) as ISocketMessageChannel;
                }
                if (channel != null)
                {
                    /*var typing = channel.TriggerTypingAsync();
                    Thread.Sleep(75 * message.Length);
                    typing.Dispose();*/
                    await channel.SendMessageAsync(message.Replace("@everyone", "@ everyone").Replace("@here", "@ here"), tts);
                    if (channel.Id != Context.Channel.Id)
                        await Context.Channel.SendMessageAsync($"Sent message to channel {channel.Name}.");
                }
                else
                {
                    await Context.Channel.SendMessageAsync("Channel is invalid!");
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Looks like you don't have permission to do that, <@{Context.User.Id}>.");
            }
        }
        [Command("user")]
        public async Task User(string text, ulong userId)
        {
            if (Context.User.Id == 366298290377195522)
                await Program.GetUser(userId).SendMessageAsync(text);
        }
        [Command("broadcast"), Summary("Sends a message into the multi-server conversation.")]
        public async Task Broadcast(string message)
        {
            if (Bot.TrustedUsers.Contains(Context.User.Id))
            {
                Conversation.LoadDatabase();
                foreach (ConversationChannel channel in Conversation.channels)
                {
                    await (Program.client.GetChannel(channel.Id) as SocketTextChannel).SendMessageAsync(message);
                    Thread.Sleep(1);
                }
            }
        }
        //[Command("pin")]
        public async Task Pin(ulong MessageId)
        {
            var message = Context.Channel.GetMessageAsync(MessageId) as IUserMessage;
            try
            {
                await message.PinAsync();
            }
            catch (Exception e)
            {
                Logger.Lawg(e.ToString());
                await Context.Channel.SendMessageAsync(CommandUtils.CensorFilepath(e.ToString()));
            }
        }
        [Command("get")]
        public async Task GetMessage(ulong Id)
        {
            var eb = new EmbedBuilder();
            var message = Context.Channel.GetMessageAsync(Id).Result;
            eb.WithDescription(message.Content);
            eb.WithTitle($"**Message From {message.Author.Username}#{message.Author.Discriminator}**");
            eb.Color = Color.Green;
            eb.WithFooter("© Mrcarrot 2018-19. All Rights Reserved.");
            if (message.Attachments.Count > 0)
            {
                eb.WithUrl(message.Attachments.First().Url);
                eb.WithImageUrl(message.Attachments.First().Url);
            }
            await Context.Channel.SendMessageAsync("", false, eb);
        }
        /*[Command("edit")]
        public async Task EditMsg(ulong msgId, string newContent)
        {
            if (Context.User.Id != 366298290377195522) return;
            IMessage msg = (Context.Channel as SocketTextChannel).GetMessageAsync(msgId).Result as IMessage;
            await msg.ModifyAsync(x => x.Content = newContent);
        }*/
        [Command("delete")]
        public async Task DeleteMsg(ulong msgId)
        {
            if (Context.User.Id != 366298290377195522) return;
            IMessage msg = Context.Channel.GetMessageAsync(msgId).Result;
            await msg.DeleteAsync();
            await Context.Message.DeleteAsync();
        }
        [Command("addreaction")]
        public async Task AddReaction(ulong message)
        {
            await (Context.Channel.GetMessageAsync(message).Result as SocketUserMessage).AddReactionAsync(new Emoji("😀"));
        }
    }
    [Group("administrator")]
    public class Administrator : ModuleBase
    {
        [Command("kick")]
        public async Task KickUser(ulong userId, string reason)
        {
            var user = Program.client.GetUser(userId) as IGuildUser;
            if (((Context.User as IGuildUser).GuildPermissions.KickMembers || Context.User.Id == 366298290377195522) && (Program.client.CurrentUser as IGuildUser).GuildPermissions.KickMembers)
            {
                await user.KickAsync(reason);
            }
            else
            {
                await Context.Channel.SendMessageAsync("One of us doesn't have that permission...");
            }
        }
        [Command("changeusernick")]
        public async Task ChangeUserNick(string user, string nick)
        {
            ulong userId = CommandUtils.GetID(user);
            if ((Context.Guild.GetUserAsync(Context.User.Id).Result.GuildPermissions.ManageNicknames || Context.User.Id == 366298290377195522))
            {
                var User = Context.Guild.GetUserAsync(userId).Result;
                await User.ModifyAsync(u => u.Nickname = nick);
                await Context.Channel.SendMessageAsync($"Set nickname of user {User.Username} to {nick}.");
            }
            else if (!Context.Guild.GetUserAsync(Context.User.Id).Result.GuildPermissions.ManageNicknames)
            {
                await Context.Channel.SendMessageAsync("You don't have permission to do that!");
            }
        }
    }
    [Group("arrest")]
    public class Arrest : ModuleBase
    {
        [Command("user")]
        public async Task ArrestUser(string user,  [Remainder]string crimes)
        {
            ulong userId = CommandUtils.GetID(user);
            string[] ArrestMessages =
            {
                $"*Police sirens in distance*\n<@{userId}>, you have the right to remain silent. You watch TV. You know the rest. You stand accused of {crimes}.",
                $"*Robot cop busts down door and runs in with a gun*\n<@{userId}>, You have the right to remain silent! SO SHUT UP!\nYou have been accused of {crimes}!"
            };
            await Context.Channel.SendMessageAsync(ArrestMessages[new Random().Next(0, 2)]);
            //Thread.Sleep(3);
            //await Context.Channel.SendMessageAsync($"-mute <@{userId}>");
        }
    }
    [Group("voice")]
    public class Voice : ModuleBase
    {
        [Command("join")]
        public async Task JoinChannel(IVoiceChannel channel = null)
        {
            channel = channel ?? (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null) { await Context.Channel.SendMessageAsync("User must be in a voice channel, or a voice channel must be passed as an argument."); return; }
            var audioClient = await channel.ConnectAsync();
        }
        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }
        private async Task SendAsync(IAudioClient client, string path)
        {
            // Create FFmpeg using the previous example
            using (var ffmpeg = CreateStream(path))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = client.CreatePCMStream(AudioApplication.Mixed))
            {
                try { await output.CopyToAsync(discord); }
                finally { await discord.FlushAsync(); }
            }
        }
        [Command("play")]
        public async Task Play()
        {

        }
    }
    /*/// <summary>
    /// Contains commands used by CyberFox to communicate with CarrotBot
    /// </summary>
    [Group("reqinfo")]
    public class ReqInfo : ModuleBase
    {
        [Command("user")]
        public async Task User(ulong Id)
        {
            string retVal = "";
            try
            {
                SocketUser u = Program.GetUser(Id);
                retVal += $"name='{u.Username}'";
                retVal += $",discriminator='{u.Discriminator}'";
                await Context.Channel.SendMessageAsync(retVal);
            }
            catch(Exception e)
            {
                string shortenedException = GetLine(e.ToString(), 1);
                await Context.Channel.SendMessageAsync($"e='{shortenedException}'");
            }
        }
        string GetLine(string text, int lineNo)
        {
            string[] lines = text.Replace("\r", "").Split('\n');
            return lines.Length >= lineNo ? lines[lineNo - 1] : null;
        }
    }*/
    [Name("SparkyMessingAround")]
    public class SparkyProofCommands : ModuleBase
    {
        [Command("erase memory")]
        public async Task EraseMem()
        {
            await Context.Channel.SendMessageAsync("Umm... no.");
        }
        [Command("who is your creator")]
        public async Task Creator()
        {
            await Context.Channel.SendMessageAsync("Mrcarrot#3305");
        }
        [Command("do you remember anything?")]
        public async Task Remember()
        {
            await Context.Channel.SendMessageAsync("Yes.");
        }
        [Command("tell me your name")]
        public async Task Name()
        {
            await Context.Channel.SendMessageAsync($"{Context.Guild.GetUserAsync(389513870835974146).Result.Nickname} (CarrotBot#5267)");
        }
    }
    [Name("transliteration")]
    public class TransliterationCommands : ModuleBase
    {
        [Command("hiragana")]
        public async Task Hiragana(string text)
        {

        }
    }
    [Group("google")]
    public class GoogleCommands : ModuleBase
    {
        [Command("search")]
        public async Task RegularSearch(string query)
        {
            if (Context.User.Id != 366298290377195522) return;
            const string apiKey = "AIzaSyAC2I1KeRLyvYYrMgqPqqI1H2QuAb_zdmQ";
            const string searchEngineId = "003470263288780838160:ty47piyybua";
            var customSearchService = new CustomsearchService(new BaseClientService.Initializer { ApiKey = apiKey });
            var listRequest = customSearchService.Cse.List(query);
            listRequest.Cx = searchEngineId;
            IList<Result> paging = new List<Result>();
            paging = listRequest.Execute().Items;
            await Context.Channel.SendMessageAsync($"{paging.ToArray()[0].Link}");
        }
        /*[Command("img"), Alias("image")]
        public async Task Image(string query)
        {
            
            const string apiKey = "AIzaSyAC2I1KeRLyvYYrMgqPqqI1H2QuAb_zdmQ";
            const string searchEngineId = "003470263288780838160:ty47piyybua";
            var customSearchService = new CustomsearchService(new BaseClientService.Initializer { ApiKey = apiKey });
            var listRequest = customSearchService.Cse.List(query);
            listRequest.Cx = searchEngineId;
            IList<Result> paging = new List<Result>();
            paging = listRequest.Execute().Items;
            await Context.Channel.SendMessageAsync($"{paging.ToArray()[0].Image.ContextLink}");
        }*/
    }
}
