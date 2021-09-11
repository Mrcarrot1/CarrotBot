using System;
using System.Collections.Generic;

namespace CarrotBot.Conversation
{
    public class ConversationChannel
    {
        public ulong Id { get; }
        public string Server { get; set; }
        public ulong GuildId { get; }
        public ulong ConversationId { get; }

        public ConversationChannel()
        {
            Server = null;
            Id = 0;
            GuildId = 0;
        }
        public ConversationChannel(ulong id, string server, ulong guildId, ulong conversationId = 0)
        {
            Server = server;
            Id = id;
            GuildId = guildId;
        }
    }
}