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
        private static DiscordChannel liveFeedChannel = null;
        public static async Task CarryOutConversation(DiscordMessage message)
        {
            ulong userId = message.Author.Id;
            bool channelIsInConversation = false;
            string Server = "";
            ConversationChannel originalChannel = null;
            for (int i = 0; i < ConversationData.ConversationChannels.Count(); i++)
            {
                if (message.Channel.Id == ConversationData.ConversationChannels[i].Id)
                {
                    channelIsInConversation = true;
                    Server = ConversationData.ConversationChannels[i].Server;
                    originalChannel = ConversationData.ConversationChannels[i];
                }
            }
            if (!channelIsInConversation)
                return;
            if (!ConversationData.AcceptedUsers.Contains(userId))
            {
                var user = await message.Channel.Guild.GetMemberAsync(userId);
                await message.DeleteAsync();
                await user.SendMessageAsync($"<@{userId}> By entering the conversation, you consent to having your data read and used by others. \nType `%conversation acceptterms` to accept these terms. Until you do, your data will not be sent.");
                Thread.Sleep(10);
                return;
            }
            if(ConversationData.BannedUsers.Contains(userId))
            {
                var user = await message.Channel.Guild.GetMemberAsync(userId);
                await message.DeleteAsync();
                await user.SendMessageAsync("You have been banned from participating in the CarrotBot Multi-Server Conversation.\nContact an administrator if you believe this to be a mistake.");
            }
            ConversationMessage msgObject = new ConversationMessage(ConversationData.GenerateMessageId(), message, originalChannel);
            for (int i = 0; i < ConversationData.ConversationChannels.Count(); i++)
            {

                if (message.Channel.Id != ConversationData.ConversationChannels[i].Id)
                {
                    var channel = Program.discord.GetChannelAsync(ConversationData.ConversationChannels[i].Id).Result;
                    string messageToSend = $"({Server}) {message.Author.Username}#{message.Author.Discriminator}: {message.Content.Replace("@", "@ ")}";
                    if (message.Attachments.Count > 0)
                        messageToSend += $"\n   Attached File: {message.Attachments.First().Url}";
                    bool Embed = false;
                    if (message.Embeds.Count > 0)
                        Embed = true;
                    try
                    {
                        if (!Embed)
                            msgObject.ChannelMessages.Add(channel.Id, await channel.SendMessageAsync(messageToSend));
                        else
                            msgObject.ChannelMessages.Add(channel.Id, await channel.SendMessageAsync(messageToSend, false, message.Embeds.First() as DiscordEmbed));
                    }
                    catch(Exception e)
                    {
                        await Program.Mrcarrot.SendMessageAsync($"Problems encountered sending message to channel {channel.Id}:\n{e.ToString()}");
                    }

                    Thread.Sleep(1);
                }
            }
            ConversationData.ConversationMessages.Add(msgObject.Id, msgObject);
            ConversationData.ConversationMessagesByOrigId.Add(message.Id, msgObject);
            ConversationData.LastMessage = msgObject;
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle($"Message from {message.Author.Username}#{message.Author.Discriminator} (via {Server})");
            eb.WithDescription(message.Content);
            eb.WithFooter($"Internal CB Id: {msgObject.Id}");
            eb.WithColor(DiscordColor.Yellow);
            msgObject.liveFeedMessage = await liveFeedChannel.SendMessageAsync(embed: eb.Build());
        }
        public static async Task SendConversationMessage(string msg)
        {
            LoadDatabase();
            for (int i = 0; i < ConversationData.ConversationChannels.Count(); i++)
            {
                try
                {
                    await Program.discord.GetChannelAsync(ConversationData.ConversationChannels[i].Id).Result.SendMessageAsync(msg);
                }
                catch (Exception e)
                {
                    await Program.Mrcarrot.SendMessageAsync($"Problems encountered sending message to channel {ConversationData.ConversationChannels[i].Id}:\n{e.ToString()}");
                }
                Thread.Sleep(5);
            }
        }
        public static async Task StartConversation()
        {
            LoadDatabase();
            await SendConversationMessage("The CarrotBot Multi-Server Conversation is now active!\nRemember: you must accept the terms (%conversation acceptterms) to enter!");
            Program.conversation = true;
        }
        public static void LoadDatabase()
        {
            liveFeedChannel = Program.discord.GetChannelAsync(818960559625732096).Result;
            ConversationData.ConversationChannels = new List<ConversationChannel>();
            foreach (string str in File.ReadAllLines($@"{Utils.conversationDataPath}/ConversationServers.csv"))
            {
                string[] values = str.Split(',');
                if (values[1] != null)
                {
                    bool ok = ulong.TryParse(values[0], out ulong Id);
                    if (ok)
                        ConversationData.ConversationChannels.Add(new ConversationChannel(Id, values[1]));
                }
            }
            foreach (string str in File.ReadAllText($@"{Utils.conversationDataPath}/AcceptedUsers.cb").Split(','))
            {
                if (ulong.TryParse(str, out ulong userId))
                    ConversationData.AcceptedUsers.Add(userId);
            }
            foreach (string str in File.ReadAllText($@"{Utils.conversationDataPath}/Administrators.cb").Split(','))
            {
                if (ulong.TryParse(str, out ulong userId))
                    ConversationData.Administrators.Add(userId);
            }
            foreach (string str in File.ReadAllText($@"{Utils.conversationDataPath}/Moderators.cb").Split(','))
            {
                if (ulong.TryParse(str, out ulong userId))
                    ConversationData.Moderators.Add(userId);
            }
            foreach (string str in File.ReadAllText($@"{Utils.conversationDataPath}/BannedUsers.cb").Split(','))
            {
                if (ulong.TryParse(str, out ulong userId))
                    ConversationData.BannedUsers.Add(userId);
            }
        }
    }
}