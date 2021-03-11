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
            foreach(KeyValuePair<ulong, DiscordMessage> msg in ChannelMessages)
            {
                await msg.Value.DeleteAsync();
            }
        }

        public async Task UpdateMessage()
        {
            /*DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle($"Message from {originalMessage.Author.Username}#{originalMessage.Author.Discriminator} (via {originalChannel.Server})");
            eb.WithDescription(originalMessage.Content);
            eb.WithFooter($"Internal CB Id: {msgObject.Id}");
            await liveFeedMessage.ModifyAsync(embed: liveFeedMessage.Embeds.First().);*/
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