using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Discord;
using Discord.WebSocket;
using System.IO;

namespace CarrotBot
{
    public class Conversation
    {
        public static List<ConversationChannel> channels = new List<ConversationChannel>();
        public static List<ulong> AcceptedUsers = new List<ulong>();
        public static void CarryOutConversation(IUserMessage message)
        {

            LoadDatabase();
            bool channelIsInConversation = false;
            string Server = "";
            for(int i = 0; i < channels.Count(); i++)
            {
                if(message.Channel.Id == channels[i].Id)
                {
                    channelIsInConversation = true;
                    Server = channels[i].Server;
                }
            }
            if (!channelIsInConversation)
                return;
            if (!AcceptedUsers.Contains(message.Author.Id))
            {
                var user = Program.GetUser(message.Author.Id);
                message.DeleteAsync();
                user.SendMessageAsync($"<@{message.Author.Id}>By entering the conversation, you consent to having your data read and used by others. Type `%terms accept` to accept these terms. Until you do, your data will not be sent.");
                Thread.Sleep(10);
                return;
            }
            for (int i = 0; i < channels.Count(); i++)
            {
                
                if (message.Channel.Id != channels[i].Id)
                {
                    var channel = Program.client.GetChannel(channels[i].Id) as ISocketMessageChannel;
                    string messageToSend = $"({Server}) {message.Author.Username}#{message.Author.Discriminator}: {message.Content.Replace("@", "@ ")}";
                    if(message.Attachments.Count > 0)
                        messageToSend += $"\n   Attached File: {message.Attachments.First().Url}";
                    bool Embed = false;
                    if (message.Embeds.Count > 0)
                        Embed = true;
                    try
                    {
                        if (!Embed)
                            channel.SendMessageAsync(messageToSend);
                        else
                            channel.SendMessageAsync(messageToSend, false, message.Embeds.First() as Embed);
                    }
                    catch(Exception e)
                    {
                        Program.Mrcarrot.SendMessageAsync($"Problems encountered sending message to channel {channel.Id}");
                    }

                    Thread.Sleep(1);
                }
            }
        }
        public static void SendConversationMessage(string msg)
        {
            LoadDatabase();
            for (int i = 0; i < channels.Count(); i++)
            {
                try
                {
                    (Program.client.GetChannel(channels[i].Id) as SocketTextChannel).SendMessageAsync(msg);
                }
                catch (Exception e)
                {
                    Program.Mrcarrot.SendMessageAsync($"Problems encountered sending message to channel {channels[i].Id}");
                }
                Thread.Sleep(5);
            }
        }
        public static void LoadDatabase()
        {
            channels = new List<ConversationChannel>();
            foreach (string str in File.ReadAllLines($@"{Environment.CurrentDirectory}/ConversationServers.csv"))
            {
                string[] values = str.Split(',');
                if (values[1] != null)
                {
                    ConversationChannel channel = new ConversationChannel();
                    bool ok = ulong.TryParse(values[0], out ulong Id);
                    if (ok)
                        channel.Id = Id;
                    channel.Server = values[1];
                    if (ok && !channels.Contains(channel))
                        channels.Add(channel);
                }
            }
            foreach(string str in File.ReadAllText($@"{Environment.CurrentDirectory}/AcceptedUsers.cb").Split(','))
            {
                if (ulong.TryParse(str, out ulong userId))
                    AcceptedUsers.Add(userId);
            }
        }
    }
    public class ConversationChannel
    {
        public ulong Id { get; set; }
        public string Server { get; set; }
    }
}
