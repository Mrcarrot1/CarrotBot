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
        public ConversationChannel originalChannel { get; }
        public Dictionary<ulong, DiscordMessage> ChannelMessages { get; set; } //A list of Discord messages by channel

        public async Task DeleteMessage(bool includeOriginal = true)
        {
            if(includeOriginal)
                await originalMessage.DeleteAsync();
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle($"[DELETED] {liveFeedMessage.Embeds.First().Title}");
            eb.WithDescription(liveFeedMessage.Embeds.First().Description);
            eb.WithFooter($"Internal CB Id: {Id}");
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
            eb.WithTitle($"[EDITED] {liveFeedMessage.Embeds.First().Title}");
            eb.AddField("Original Content:", liveFeedMessage.Embeds.First().Description);
            eb.AddField("Edited Content:", originalMessage.Content);
            eb.WithFooter($"Internal CB Id: {Id}");
            eb.WithColor(DiscordColor.Yellow);
            await liveFeedMessage.ModifyAsync(embed: eb.Build());
            foreach(KeyValuePair<ulong, DiscordMessage> msg in ChannelMessages)
            {
                await msg.Value.ModifyAsync($"({originalChannel.Server}) {originalMessage.Author.Username}#{originalMessage.Author.Discriminator}: {originalMessage.Content}");
            }
        }
        public ConversationMessage(ulong id, DiscordMessage msg, ConversationChannel chnOrig)
        {
            Id = id;
            originalMessage = msg;
            originalChannel = chnOrig;
            ChannelMessages = new Dictionary<ulong, DiscordMessage>();
        }
    }
}