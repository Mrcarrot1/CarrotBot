using System;
using System.Collections.Generic;

namespace CarrotBot.Conversation
{
    public class ConversationChannel
    {
        public ulong Id { get; set; }
        public string Server { get; set; }

        public ConversationChannel()
        {
            Server = null;
            Id = 0;
        }
        public ConversationChannel(ulong id, string server)
        {
            Server = server;
            Id = id;
        }
    }
}