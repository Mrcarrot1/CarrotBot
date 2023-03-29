using System;

namespace CarrotBot.Conversation
{
    public class PreVerifiedUser
    {
        public ulong Id { get; }
        public int MessagesSent { get; set; }
        public DateTimeOffset LastMessageSentTime { get; set; }

        public PreVerifiedUser(ulong id, int messagesSent, long lastMessageTimestamp)
        {
            Id = id;
            MessagesSent = messagesSent;
            LastMessageSentTime = DateTimeOffset.FromUnixTimeSeconds(lastMessageTimestamp);
        }
        public PreVerifiedUser(ulong id, int messagesSent)
        {
            Id = id;
            MessagesSent = messagesSent;
            LastMessageSentTime = DateTimeOffset.Now;
        }
        public PreVerifiedUser(ulong id)
        {
            Id = id;
            MessagesSent = 0;
            LastMessageSentTime = DateTimeOffset.Now;
        }
    }
}