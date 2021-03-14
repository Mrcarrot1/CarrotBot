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
            var user = await message.Channel.Guild.GetMemberAsync(userId);
            if (!ConversationData.AcceptedUsers.Contains(userId))
            {
                await message.DeleteAsync();
                await user.SendMessageAsync($"<@{userId}> By entering the conversation, you consent to having your data read and used by others. \nType `%conversation acceptterms` to accept these terms. Until you do, your data will not be sent.");
                Thread.Sleep(10);
                return;
            }
            if(ConversationData.BannedUsers.Contains(userId))
            {
                await message.DeleteAsync();
                await user.SendMessageAsync("You have been banned from participating in the CarrotBot Multi-Server Conversation.\nContact an administrator if you believe this to be a mistake.");
            }
            ConversationMessage msgObject = new ConversationMessage(ConversationData.GenerateMessageId(), message, user, originalChannel);
            DiscordEmbedBuilder eb2 = new DiscordEmbedBuilder();
            string Title = $"{message.Author.Username}#{message.Author.Discriminator}";
            string Footer = $"Via {Server}";
            if(ConversationData.Administrators.Contains(userId))
            {
                Title = $"[ADMIN] {Title}";
                eb2.WithColor(DiscordColor.Blue);
                Footer = $"This user is a conversation administrator ・ {Footer}";
            }
            if(ConversationData.Moderators.Contains(userId))
            {
                Title = $"[MOD] {Title}";
                eb2.WithColor(DiscordColor.Magenta);
                Footer = $"This user is a conversation moderataor ・ {Footer}";
            }
            if(userId == 366298290377195522)
            {
                Title = $"[DEV] {Title}";
                eb2.WithColor(DiscordColor.Green);
                Footer = $"This user is the developer of CarrotBot ・　{Footer}";
            }
            eb2.WithTitle($"{message.Author.Username}#{message.Author.Discriminator}");
            eb2.WithThumbnailUrl(message.Author.AvatarUrl);
            eb2.WithFooter($"Via {Server}");
            eb2.WithDescription(message.Content);
            if (message.Attachments.Count > 0)
            {
                eb2.WithImageUrl(message.Attachments[0].Url);
                eb2.AddField("Attachment URL", message.Attachments[0].Url);
            }
            DiscordEmbed embed = eb2.Build();
            for (int i = 0; i < ConversationData.ConversationChannels.Count(); i++)
            {
                if (message.Channel.Id != ConversationData.ConversationChannels[i].Id)
                {
                    var channel = Program.discord.GetChannelAsync(ConversationData.ConversationChannels[i].Id).Result;
                    
                    /*bool Embed = false;
                    if (message.Embeds.Count > 0)
                        Embed = true;
                    try
                    {
                        if (!Embed)
                            msgObject.ChannelMessages.Add(channel.Id, await channel.SendMessageAsync(messageToSend));
                        else
                            msgObject.ChannelMessages.Add(channel.Id, await channel.SendMessageAsync(messageToSend, false, message.Embeds[0] as DiscordEmbed));
                    }*/
                    try
                    {
                        msgObject.ChannelMessages.Add(channel.Id, await channel.SendMessageAsync(embed: embed));
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
            eb.WithFooter($"Internal CB Id: {msgObject.Id}\nUser Id: {message.Author.Id}");
            eb.WithColor(DiscordColor.Green);
            msgObject.liveFeedMessage = await liveFeedChannel.SendMessageAsync(embed: eb.Build());
            msgObject.Embed = embed;
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