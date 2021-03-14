using System;
using System.Threading.Tasks;
using System.Collections.Generic;
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

        public async Task DeleteMessage(bool includeOriginal = true)
        {
            if(includeOriginal)
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
            foreach(KeyValuePair<ulong, DiscordMessage> msg in ChannelMessages)
            {
                await msg.Value.DeleteAsync();
            }
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
            eb2.WithTitle(Embed.Title);
            eb2.WithThumbnailUrl(Author.AvatarUrl);
            eb2.WithFooter($"{Embed.Footer} ・ Edited");
            eb2.WithDescription(originalMessage.Content);
            eb2.WithColor(Embed.Color);
            if (originalMessage.Attachments.Count > 0)
            {
                eb2.WithImageUrl(originalMessage.Attachments[0].Url);
                eb2.AddField("Attachment URL", originalMessage.Attachments[0].Url);
            }
            DiscordEmbed embed = eb2.Build();
            foreach(KeyValuePair<ulong, DiscordMessage> msg in ChannelMessages)
            {
                //await msg.Value.ModifyAsync($"({originalChannel.Server}) {originalMessage.Author.Username}#{originalMessage.Author.Discriminator}: {originalMessage.Content}");
                await msg.Value.ModifyAsync(embed: embed);
            }
        }
        public ConversationMessage(ulong id, DiscordMessage msg, DiscordMember author, ConversationChannel chnOrig)
        {
            Id = id;
            originalMessage = msg;
            originalChannel = chnOrig;
            Author = author;
            ChannelMessages = new Dictionary<ulong, DiscordMessage>();
        }
    }
}