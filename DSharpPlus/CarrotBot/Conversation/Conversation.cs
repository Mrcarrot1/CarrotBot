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
        public static DiscordChannel liveFeedChannel = null;
        public static DiscordChannel embedsChannel = null;
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
            if (ConversationData.BannedUsers.Contains(userId))
            {
                await message.DeleteAsync();
                await user.SendMessageAsync("You have been banned from participating in the CarrotBot Multi-Server Conversation.\nContact an administrator if you believe this to be a mistake.");
                return;
            }
            if (ConversationData.LastMessage != null && false)
            {
                if (ConversationData.LastMessage.Author.Id == userId && ConversationData.LastMessage.originalChannel.Id == originalChannel.Id)
                {
                    //Messages in the same embed are separated by the zero width space (​)
                    if (ConversationData.LastMessage.Embed.Description.Length + message.Content.Length <= 2046)
                    {
                        ConversationMessage secondMsgObject = new ConversationMessage(ConversationData.GenerateMessageId(), message, user, originalChannel, ConversationData.LastMessage.IndexInEmbed + 1);
                        DiscordEmbedBuilder eb3 = new DiscordEmbedBuilder(ConversationData.LastMessage.Embed);
                        eb3.WithDescription(eb3.Description + "​\n" + message.Content); //Separate messages by a zero-width space followed by a line break
                        var embed3 = eb3.Build();
                        secondMsgObject.Embed = embed3;
                        secondMsgObject.ChannelMessages = ConversationData.LastMessage.ChannelMessages;
                        ConversationData.LastMessage = secondMsgObject;
                        foreach (KeyValuePair<ulong, DiscordMessage> msg in ConversationData.LastMessage.ChannelMessages)
                        {
                            //await msg.Value.ModifyAsync($"({originalChannel.Server}) {originalMessage.Author.Username}#{originalMessage.Author.Discriminator}: {originalMessage.Content}");
                            await msg.Value.ModifyAsync(embed: embed3);
                        }
                        await ConversationData.LastMessage.EmbedMessage.ModifyAsync(embed: embed3);
                        DiscordEmbedBuilder eb4 = new DiscordEmbedBuilder();
                        eb4.WithTitle($"Message from {message.Author.Username}#{message.Author.Discriminator} (via {Server})");
                        eb4.WithDescription(message.Content);
                        eb4.WithFooter($"Internal CB Id: {secondMsgObject.Id}\nUser Id: {message.Author.Id}");
                        eb4.WithColor(DiscordColor.Green);
                        secondMsgObject.liveFeedMessage = await liveFeedChannel.SendMessageAsync(embed: eb4.Build());
                        secondMsgObject.PreviousMessage = ConversationData.LastMessage;
                        secondMsgObject.EmbedMessage = ConversationData.LastMessage.EmbedMessage;
                        await secondMsgObject.EmbedMessage.ModifyAsync(embed: embed3);
                        ConversationData.ConversationMessages.Add(secondMsgObject.Id, secondMsgObject);
                        ConversationData.ConversationMessagesByOrigId.Add(message.Id, secondMsgObject);
                        ConversationData.LastMessage.UpdateEmbed(false, true);
                        ConversationData.LastMessage.NextMessage = secondMsgObject;
                        ConversationData.LastMessage = secondMsgObject;
                        return;
                    }

                }
            }
            DiscordEmbedBuilder eb2 = new DiscordEmbedBuilder();
            eb2.WithColor(DiscordColor.LightGray);
            if (ConversationData.VerifiedUsers.Contains(userId))
                eb2.WithColor(Utils.CBGreen);
            else if (ConversationData.PreVerifiedUsers.ContainsKey(userId))
            {
                if (DateTimeOffset.Now.Subtract(ConversationData.PreVerifiedUsers[userId].LastMessageSentTime) > new TimeSpan(0, 0, 60))
                {
                    ConversationData.PreVerifiedUsers[userId].MessagesSent++;
                    ConversationData.PreVerifiedUsers[userId].LastMessageSentTime = DateTime.Now;
                    if (ConversationData.PreVerifiedUsers[userId].MessagesSent >= 20)
                    {
                        ConversationData.VerifiedUsers.Add(userId);
                        ConversationData.PreVerifiedUsers.Remove(userId);
                        eb2.WithColor(Utils.CBGreen);
                    }
                }
                ConversationData.WriteDatabase();
            }
            else
            {
                ConversationData.PreVerifiedUsers.Add(userId, new PreVerifiedUser(userId, 1));
                ConversationData.WriteDatabase();
            }
            ConversationMessage msgObject = new ConversationMessage(ConversationData.GenerateMessageId(), message, user, originalChannel);
            string Title = $"{message.Author.Username}#{message.Author.Discriminator}";
            string Footer = $"Via {Server}";

            if (userId == 366298290377195522)
            {
                Title = $"[DEV] {Title}";
                eb2.WithColor(DiscordColor.Green);
                Footer = $"CarrotBot Developer ・ {Footer}";
            }
            else if (ConversationData.Administrators.Contains(userId))
            {
                Title = $"[ADMIN] {Title}";
                eb2.WithColor(DiscordColor.Blue);
                Footer = $"Conversation Administrator ・ {Footer}";
            }
            else if (ConversationData.Moderators.Contains(userId))
            {
                Title = $"[MOD] {Title}";
                eb2.WithColor(DiscordColor.HotPink);
                Footer = $"Conversation Moderator ・ {Footer}";
            }
            //eb2.WithTitle(Title);
            eb2.WithAuthor(Title, icon_url: user.AvatarUrl);

            eb2.WithFooter(Footer);
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
                    catch (Exception e)
                    {
                        await Program.Mrcarrot.SendMessageAsync($"Problems encountered sending message to channel {channel.Id}:\n{e.ToString()}");
                    }

                    Thread.Sleep(1);
                }
            }
            ConversationData.ConversationMessages.Add(msgObject.Id, msgObject);
            ConversationData.ConversationMessagesByOrigId.Add(message.Id, msgObject);
            msgObject.PreviousMessage = ConversationData.LastMessage;
            ConversationData.LastMessage = msgObject;
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle($"Message from {message.Author.Username}#{message.Author.Discriminator} (via {Server})");
            eb.WithDescription(message.Content);
            eb.WithFooter($"Internal CB Id: {msgObject.Id}\nUser Id: {message.Author.Id}");
            eb.WithColor(DiscordColor.Green);
            msgObject.liveFeedMessage = await liveFeedChannel.SendMessageAsync(embed: eb.Build());
            msgObject.Embed = embed;
            msgObject.EmbedMessage = await embedsChannel.SendMessageAsync(embed: embed);
        }
        public static async Task SendConversationMessage(string msg)
        {
            ConversationData.LoadDatabase();
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
        public static async Task StartConversation(bool alert = true)
        {
            try
            {
                ConversationData.LoadDatabase();
                if (alert)
                {
                    if (!Program.isBeta)
                        await SendConversationMessage("The CarrotBot Multi-Server Conversation is now active!\nRemember: you must accept the terms (%conversation acceptterms) to enter!");
                    else
                        await SendConversationMessage("The CarrotBot Multi-Server Conversation Beta is now active!\nRemember: you must accept the terms (%conversation acceptterms) to enter!\nThis is a beta version and as such is less stable and more frequently updated than the main conversation.");
                }
                Program.conversation = true;
            }
            catch (Exception e)
            {
                await Program.Mrcarrot.SendMessageAsync(e.ToString());
            }
        }
    }
}