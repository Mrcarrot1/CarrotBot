using System;
using System.Collections.Generic;

namespace CarrotBot.Conversation
{
    public class ConversationChannel
    {
        /// <summary>
        /// Discord channel Id.
        /// </summary>
        /// <value></value>
        public ulong Id { get; }
        /// <summary>
        /// The callsign of the server the channel is in.
        /// </summary>
        /// <value></value>
        public string CallSign { get; set; }
        /// <summary>
        /// The Discord Id of the guild the channel is in.
        /// </summary>
        /// <value></value>
        public ulong GuildId { get; }
        public ulong ConversationId { get; }

        public ConversationChannel()
        {
            CallSign = null;
            Id = 0;
            GuildId = 0;
        }
        public ConversationChannel(ulong id, string server, ulong guildId, ulong conversationId = 0)
        {
            CallSign = server;
            Id = id;
            GuildId = guildId;
        }
    }
}