//Unfinished reaction role module-
//Will be finished in a later release.

using DSharpPlus.Entities;

namespace CarrotBot.Modules.ReactionRoles
{
    public class ReactionRole
    {
        /// <summary>
        /// The role to grant when the reaction is selected
        /// </summary>
        /// <value></value>
        public DiscordRole Role { get; }
        /// <summary>
        /// The message the reaction is on
        /// </summary>
        /// <value></value>
        public DiscordMessage Message { get; }
        /// <summary>
        /// The reaction emote to add
        /// </summary>
        /// <value></value>
        public DiscordEmoji Emote { get; }

        public ReactionRole(DiscordRole role, DiscordMessage message, DiscordEmoji emote)
        {
            Role = role;
            Message = message;
            Emote = emote;
        }
    }
}