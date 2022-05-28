using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using System.Net.Http;
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
            try
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
                        Server = ConversationData.ConversationChannels[i].CallSign;
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
                //Check for certain offensive words-
                //For obvious reasons, these are not in the source code. They are kept locally in the database.
                //For a list, contact Mrcarrot.
                foreach (string str in ConversationData.BannedWords)
                {
                    if (message.Content.ToLower().Contains(str))
                    {
                        await message.DeleteAsync();
                        await user.SendMessageAsync("Your message has been removed for containing an offensive word.\nContact a CarrotBot administrator if you believe this to be a mistake.");
                    }
                }
                /*if (ConversationData.LastMessage != null)
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
                }*/
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

                if (Program.BotGuild.Members.ContainsKey(userId))
                {
                    if (Program.BotGuild.Members[userId].Roles.Any(x => x.Id == 907824766168203295))
                    {
                        eb2.WithColor(Utils.CBOrange);
                        Footer = $"Patreon Supporter ・ {Footer}";
                    }
                }

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
                eb2.WithAuthor(Title, iconUrl: user.AvatarUrl);

                eb2.WithFooter(Footer);
                eb2.WithDescription(message.Content);
                if (message.Attachments.Count > 0)
                {
                    eb2.WithImageUrl(message.Attachments[0].ProxyUrl);
                    eb2.Description += $"\n[Attachment Link]({message.Attachments[0].ProxyUrl})";
                }
                if (message.Stickers.Count > 0)
                {
                    eb2.WithImageUrl(message.Stickers.First().StickerUrl);
                }

                //Scan the message for image URLs and set the first one found to the embed's thumbnail URL
                Regex URLRegex = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)");
                foreach (Match match in URLRegex.Matches(message.Content))
                {
                    if (Utils.IsImageUrl(match.Value))
                    {
                        eb2.WithImageUrl(match.Value);
                        break;
                    }
                }
                DiscordEmbed embed = eb2.Build();

                ConversationMessage RefMsg = null;
                if (message.ReferencedMessage != null && message.MessageType == MessageType.Reply)
                {
                    var replyMsg = message.ReferencedMessage;
                    if (replyMsg.Author.Id == Program.discord.CurrentUser.Id)
                    {
                        if (ConversationData.ConversationMessagesByOutId.ContainsKey(replyMsg.Id))
                        {
                            RefMsg = ConversationData.ConversationMessagesByOutId[replyMsg.Id];
                        }
                    }
                    else
                    {
                        if (ConversationData.ConversationMessagesByOrigId.ContainsKey(replyMsg.Id))
                        {
                            RefMsg = ConversationData.ConversationMessagesByOrigId[replyMsg.Id];
                        }
                    }
                }
                if (RefMsg != null)
                {
                    try
                    {
                        try
                        {
                            if (message.ChannelId != RefMsg.originalChannel.Id)
                                msgObject.ChannelMessages.Add(RefMsg.originalChannel.Id, await RefMsg.originalMessage.RespondAsync(embed: embed));
                        }
                        catch { throw; }
                        foreach (DiscordMessage message1 in RefMsg.ChannelMessages.Values)
                        {
                            try
                            {
                                if (message.ChannelId != message1.ChannelId)
                                    msgObject.ChannelMessages.Add(message1.ChannelId, await message1.RespondAsync(embed: embed));
                                Thread.Sleep(1);
                            }
                            catch { throw; }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log(e.ToString(), Logger.CBLogLevel.EXC);
                    }
                }

                for (int i = 0; i < ConversationData.ConversationChannels.Count(); i++)
                {
                    if (message.Channel.Id != ConversationData.ConversationChannels[i].Id)
                    {
                        //Check if the guild in question has a shard associated with it-
                        //If it doesn't, the guild has most likely either been deleted or removed the bot.
                        //So we remove the guild from the database.
                        var shard = Program.discord.GetShard(ConversationData.ConversationChannels[i].GuildId);
                        if (shard == null)
                        {
                            Logger.Log($"Conversation: Shard not found for guild {ConversationData.ConversationChannels[i].GuildId}. Assuming invalid guild.", Logger.CBLogLevel.WRN);
                            ConversationData.ConversationChannels.RemoveAt(i);
                            ConversationData.WriteDatabase();
                            i--;
                            continue;
                        }
                        //Then we do the same for the channel-
                        //This requires a slightly different approach as GetChannelAsync throws an exception instead of returning null if the channel is not found.
                        try
                        {
                            var channel = shard.GetChannelAsync(ConversationData.ConversationChannels[i].Id).Result;
                            if (RefMsg == null || (!RefMsg.ChannelMessages.Any(x => x.Value.ChannelId == channel.Id) && RefMsg.originalChannel.Id != channel.Id))
                            {
                                var outMessage = await channel.SendMessageAsync(embed: embed);
                                msgObject.ChannelMessages.Add(channel.Id, outMessage);
                                ConversationData.ConversationMessagesByOutId.Add(outMessage.Id, msgObject);
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Log($"Conversation: Channel not found for server {ConversationData.ConversationChannels[i].CallSign}. Assuming invalid channel.", Logger.CBLogLevel.ERR);
                            Logger.Log(e.ToString(), Logger.CBLogLevel.EXC);
                            ConversationData.ConversationChannels.RemoveAt(i);
                            ConversationData.WriteDatabase();
                            i--;
                            continue;
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
                ConversationData.MessageDataChangedSinceLastWrite = true;
                //Write the message database if it is less than 5 minutes before midnight
                if ((DateTimeOffset.Now + new TimeSpan(0, 5, 0)).Day > DateTimeOffset.Now.Day)
                {
                    ConversationData.WriteMessageData(true);
                }
            }
            catch (Exception e)
            {
                await Program.Mrcarrot.SendMessageAsync(e.ToString());
            }
        }
        public static async Task SendConversationMessage(string msg)
        {
            ConversationData.LoadDatabase();
            for (int i = 0; i < ConversationData.ConversationChannels.Count(); i++)
            {
                try
                {
                    await Program.discord.GetShard(ConversationData.ConversationChannels[i].GuildId).GetChannelAsync(ConversationData.ConversationChannels[i].Id).Result.SendMessageAsync(msg);
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