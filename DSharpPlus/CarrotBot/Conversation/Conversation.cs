using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using DSharpPlus;
using DSharpPlus.Entities;
using System.IO;

namespace CarrotBot.Conversation
{
    public class Conversation
    {
        public static List<ConversationChannel> ConversationChannels = new List<ConversationChannel>();
        public static List<ulong> AcceptedUsers = new List<ulong>();
        public static async Task CarryOutConversation(DiscordMessage message)
        {
            ulong userId = message.Author.Id;
            bool channelIsInConversation = false;
            string Server = "";
            for (int i = 0; i < ConversationChannels.Count(); i++)
            {
                if (message.Channel.Id == ConversationChannels[i].Id)
                {
                    channelIsInConversation = true;
                    Server = ConversationChannels[i].Server;
                }
            }
            if (!channelIsInConversation)
                return;
            if (!AcceptedUsers.Contains(userId))
            {
                var user = await message.Channel.Guild.GetMemberAsync(userId);
                await message.DeleteAsync();
                await user.SendMessageAsync($"<@{userId}> By entering the conversation, you consent to having your data read and used by others. \nType `%conversation acceptterms` to accept these terms. Until you do, your data will not be sent.");
                Thread.Sleep(10);
                return;
            }
            for (int i = 0; i < ConversationChannels.Count(); i++)
            {

                if (message.Channel.Id != ConversationChannels[i].Id)
                {
                    var channel = Program.discord.GetChannelAsync(ConversationChannels[i].Id).Result;
                    string messageToSend = $"({Server}) {message.Author.Username}#{message.Author.Discriminator}: {message.Content.Replace("@", "@ ")}";
                    if (message.Attachments.Count > 0)
                        messageToSend += $"\n   Attached File: {message.Attachments.First().Url}";
                    bool Embed = false;
                    if (message.Embeds.Count > 0)
                        Embed = true;
                    try
                    {
                        if (!Embed)
                            await channel.SendMessageAsync(messageToSend);
                        else
                            await channel.SendMessageAsync(messageToSend, false, message.Embeds.First() as DiscordEmbed);
                    }
                    catch(Exception e)
                    {
                        await Program.Mrcarrot.SendMessageAsync($"Problems encountered sending message to channel {channel.Id}:\n{e.ToString()}");
                    }

                    Thread.Sleep(1);
                }
            }
        }
        public static async Task SendConversationMessage(string msg)
        {
            LoadDatabase();
            for (int i = 0; i < ConversationChannels.Count(); i++)
            {
                try
                {
                    await Program.discord.GetChannelAsync(ConversationChannels[i].Id).Result.SendMessageAsync(msg);
                }
                catch (Exception e)
                {
                    await Program.Mrcarrot.SendMessageAsync($"Problems encountered sending message to channel {ConversationChannels[i].Id}:\n{e.ToString()}");
                }
                Thread.Sleep(5);
            }
        }
        public static async Task StartConversation()
        {
            LoadDatabase();
            await SendConversationMessage("WARNING: THIS IS A HIGHLY UNSTABLE BETA\nThe CarrotBot Multi-Server Conversation is now active!\nRemember: you must accept the terms (%conversation acceptterms) to enter!");
            Program.conversation = true;
        }
        public static void LoadDatabase()
        {
            ConversationChannels = new List<ConversationChannel>();
            foreach (string str in File.ReadAllLines($@"{Utils.localDataPath}/ConversationServers.csv"))
            {
                string[] values = str.Split(',');
                if (values[1] != null)
                {
                    bool ok = ulong.TryParse(values[0], out ulong Id);
                    if (ok)
                        ConversationChannels.Add(new ConversationChannel(Id, values[1]));
                }
            }
            foreach (string str in File.ReadAllText($@"{Utils.localDataPath}/AcceptedUsers.cb").Split(','))
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

        public ConversationChannel()
        {
            Server = null;
            Id = 0;
        }
        public ConversationChannel(ulong id, string server)
        {
            Server = server;
            Id = id;
        }
    }
}