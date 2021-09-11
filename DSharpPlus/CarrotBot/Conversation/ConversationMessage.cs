//This file contains some of the worst code I have ever written.
//Any improvements that can be made retaining or increasing the functionality would be greatly appreciated.
//And a quick note- several strings in this file contain zero-width spaces.
//I don't know exactly which ones.
//If you lose the will to live upon seeing some of this code, I do not blame you.
//Good luck.
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.Entities;
using System.Linq;

namespace CarrotBot.Conversation
{
    public class ConversationMessage
    {
        public ulong Id { get; } //Id in CB's internal system- NOT A DISCORD MESSAGE ID
        public DiscordMessage originalMessage { get; }
        public DiscordMessage liveFeedMessage { get; set; }
        public DiscordEmbed Embed { get; set; }
        public DiscordMember Author { get; }
        public ConversationChannel originalChannel { get; }
        public Dictionary<ulong, DiscordMessage> ChannelMessages { get; set; } //A list of Discord messages by channel
        public int IndexInEmbed { get; set; }
        public ConversationMessage PreviousMessage { get; set; }
        public ConversationMessage NextMessage { get; set; }
        public DiscordMessage EmbedMessage { get; set; }
        public async Task DeleteMessage(bool includeOriginal = true)
        {
            //This try-catch simply serves to make sure that we don't try to delete a message that already has been.
            //Otherwise, that would be a fairly big issue.
            try
            {
                if(includeOriginal)
                await originalMessage.DeleteAsync();
                DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
                try
                {
                    eb.WithTitle($"[DELETED] {liveFeedMessage.Embeds[0].Title}");
                    eb.WithDescription(liveFeedMessage.Embeds[0].Description);
                    if(liveFeedMessage.Embeds[0].Fields != null)
                    {
                        foreach(DiscordEmbedField f in liveFeedMessage.Embeds[0].Fields)
                        {
                            eb.AddField(f.Name, f.Value);
                        }
                    }
                    eb.WithFooter(liveFeedMessage.Embeds[0].Footer.Text);
                    eb.WithColor(DiscordColor.Red);
                    await liveFeedMessage.ModifyAsync(embed: eb.Build());
                }
                catch(Exception e) 
                { 
                    await Program.Mrcarrot.SendMessageAsync($"{e.ToString()}");
                }
                foreach(KeyValuePair<ulong, DiscordMessage> msg in ChannelMessages)
                {
                    await msg.Value.DeleteAsync();
                }
            }
            catch
            {

            }
            /*if(includeOriginal)
                await originalMessage.DeleteAsync();
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle($"[DELETED] {liveFeedMessage.Embeds[0].Title}");
            eb.WithDescription(liveFeedMessage.Embeds[0].Description);
            foreach(DiscordEmbedField f in liveFeedMessage.Embeds[0].Fields)
            {
                eb.AddField(f.Name, f.Value);
            }
            eb.WithFooter(liveFeedMessage.Embeds[0].Footer.Text);
            eb.WithColor(DiscordColor.Red);
            await liveFeedMessage.ModifyAsync(embed: eb.Build());
            string[] messagesInEmbed = EmbedMessage.Embeds[0].Description.Split("​");
            if(messagesInEmbed.Length <= 1)
            {
                foreach(KeyValuePair<ulong, DiscordMessage> msg in ChannelMessages)
                {
                    await msg.Value.DeleteAsync();
                }
            }
            else if(messagesInEmbed.Length == IndexInEmbed + 1) //Determine if it's the last one in the thing
            {
                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder(EmbedMessage.Embeds[0]);
                embedBuilder.WithDescription(embedBuilder.Description.Replace($"​{messagesInEmbed[IndexInEmbed]}", "")); //Note that this actually is an empty string, though the one before it begins with a zero-width space
                DiscordEmbed embed = embedBuilder.Build();
                await EmbedMessage.ModifyAsync(embed: embed);
                UpdateEmbed(false, true);
                foreach(KeyValuePair<ulong, DiscordMessage> msg in ChannelMessages)
                {
                    await msg.Value.ModifyAsync(embed: embed);
                }
            }
            else
            {
                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder(EmbedMessage.Embeds[0]);
                embedBuilder.WithDescription(embedBuilder.Description.Replace($"​{messagesInEmbed[IndexInEmbed]}", ""));
                DiscordEmbed embed = embedBuilder.Build();
                await EmbedMessage.ModifyAsync(embed: embed);
                UpdateEmbed(false, true);
                foreach(KeyValuePair<ulong, DiscordMessage> msg in ChannelMessages)
                {
                    await msg.Value.ModifyAsync(embed: embed);
                }
                NextMessage.DecrementIndex();
            }*/
        }

        public async Task UpdateMessage()
        {
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle($"[EDITED] {liveFeedMessage.Embeds[0].Title}");
            eb.AddField("Original Content:", liveFeedMessage.Embeds[0].Description);
            eb.AddField("Edited Content:", originalMessage.Content);
            eb.WithFooter(liveFeedMessage.Embeds[0].Footer.Text);
            eb.WithColor(DiscordColor.Yellow);
            await liveFeedMessage.ModifyAsync(embed: eb.Build());
            DiscordEmbedBuilder eb2 = new DiscordEmbedBuilder();
            eb2.WithAuthor(Embed.Author.Name, Author.AvatarUrl);
            eb2.WithFooter($"{Embed.Footer.Text} ・ Edited");
            eb2.WithDescription(originalMessage.Content);
            eb2.WithColor((DiscordColor)Embed.Color);
            if (originalMessage.Attachments.Count > 0)
            {
                eb2.WithImageUrl(originalMessage.Attachments[0].Url);
                eb2.AddField("Attachment URL", originalMessage.Attachments[0].Url);
            }
            Regex URLRegex = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)");
            foreach(Match match in URLRegex.Matches(originalMessage.Content))
            {
                if(Utils.IsImageUrl(match.Value))
                {
                    eb2.WithImageUrl(match.Value);
                    break;
                }
            }
            DiscordEmbed embed = eb2.Build();
            foreach(KeyValuePair<ulong, DiscordMessage> msg in ChannelMessages)
            {
                //await msg.Value.ModifyAsync($"({originalChannel.Server}) {originalMessage.Author.Username}#{originalMessage.Author.Discriminator}: {originalMessage.Content}");
                await msg.Value.ModifyAsync(embed: embed);
            }
            /*DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle($"[EDITED] {liveFeedMessage.Embeds[0].Title}");
            eb.AddField("Original Content:", liveFeedMessage.Embeds[0].Description);
            eb.AddField("Edited Content:", originalMessage.Content);
            eb.WithFooter(liveFeedMessage.Embeds[0].Footer.Text);
            eb.WithColor(DiscordColor.Yellow);
            await liveFeedMessage.ModifyAsync(embed: eb.Build());
            DiscordEmbedBuilder eb2 = new DiscordEmbedBuilder(EmbedMessage.Embeds[0]);
            //eb2.WithDescription(eb2.Description.Replace(eb2.Description.Split("​")[IndexInEmbed], "\n" + originalMessage.Content));
            eb2.WithDescription(originalMessage.Content);
            if(!eb2.Footer.Text.Contains("Edited"))
                eb2.WithFooter($"{eb2.Footer.Text} ・ Edited");
            if (originalMessage.Attachments.Count > 0)
            {
                eb2.WithImageUrl(originalMessage.Attachments[0].Url);
                eb2.AddField("Attachment URL", originalMessage.Attachments[0].Url);
            }
            DiscordEmbed embed = eb2.Build();
            await EmbedMessage.ModifyAsync(embed: embed);
            //UpdateEmbed(false, true);
            foreach(KeyValuePair<ulong, DiscordMessage> msg in ChannelMessages)
            {
                //await msg.Value.ModifyAsync($"({originalChannel.Server}) {originalMessage.Author.Username}#{originalMessage.Author.Discriminator}: {originalMessage.Content}");
                await msg.Value.ModifyAsync(embed: embed);
            }*/
        }
        public void DecrementIndex()
        {
            IndexInEmbed -= 1;
            if(NextMessage != null)
            {
                if(NextMessage.IndexInEmbed > 0)
                    NextMessage.DecrementIndex();
            }
        }
        public void UpdateEmbed(bool singleDirection, bool goForward)
        {
            Embed = EmbedMessage.Embeds[0];
            if(!singleDirection)
            {
                if(NextMessage != null)
                    NextMessage.UpdateEmbed(true, true);
                if(PreviousMessage != null)
                    NextMessage.UpdateEmbed(true, false);
                return;
            }
            if(goForward && NextMessage != null)
            {
                NextMessage.UpdateEmbed(true, true);
            }
            if(!goForward && PreviousMessage != null)
            {
                PreviousMessage.UpdateEmbed(true, false);
            }
        }
        public ConversationMessage(ulong id, DiscordMessage msg, DiscordMember author, ConversationChannel chnOrig)
        {
            Id = id;
            originalMessage = msg;
            originalChannel = chnOrig;
            Author = author;
            ChannelMessages = new Dictionary<ulong, DiscordMessage>();
            IndexInEmbed = 0;
        }
        public ConversationMessage(ulong id, DiscordMessage msg, DiscordMember author, ConversationChannel chnOrig, int indexInEmbed)
        {
            Id = id;
            originalMessage = msg;
            originalChannel = chnOrig;
            Author = author;
            ChannelMessages = new Dictionary<ulong, DiscordMessage>();
            IndexInEmbed = indexInEmbed;
        }
    }
}