using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using KarrotObjectNotation;

namespace CarrotBot.Conversation
{
    /// <summary>
    /// A conversation outside of the main "official" one.
    /// </summary>
    public class AdditionalConversation
    {
        public static List<AdditionalConversation> Conversations { get; private set; }

        public ulong Id { get; }
        public string Name { get; }
        public Dictionary<ulong, ConversationChannel> Channels { get; private set; }
        public async Task SendMessageAsync(string message)
        {
            foreach (ConversationChannel channel in Channels.Values)
            {
                await Program.discord.GetShard(channel.GuildId).GetChannelAsync(channel.Id).Result.SendMessageAsync(message);
            }
        }

        public async Task SendMessageAsync(DiscordMessage message)
        {
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder
            {
                Description = message.Content,
            }.WithAuthor($"{message.Author.Username}#{message.Author.Discriminator}", iconUrl: message.Author.GetAvatarUrl(ImageFormat.Auto))
            .WithFooter($"Via {Channels[message.Channel.Guild.Id].CallSign}")
            .WithColor(DiscordColor.LightGray);
            if (ConversationData.VerifiedUsers.Contains(message.Author.Id))
            {
                eb.WithColor(Utils.CBGreen);
            }
            if (ConversationData.Moderators.Contains(message.Author.Id))
            {
                eb.WithColor(DiscordColor.HotPink);
                eb.WithFooter($"Conversation Moderator ・ {eb.Footer}");
            }
            if (ConversationData.Administrators.Contains(message.Author.Id))
            {
                eb.WithColor(DiscordColor.Blue);
                eb.WithFooter($"Conversation Administrator ・ {eb.Footer}");
            }
            if (message.Author.Id == 366298290377195522)
            {
                eb.WithColor(DiscordColor.Green);
                eb.WithFooter($"CarrotBot Developer ・ {eb.Footer}");
            }
            await Task.Run(() => null);
        }
    }
}