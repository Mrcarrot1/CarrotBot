using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace CarrotBot.Conversation
{
    /// <summary>
    /// Checks to see if a user has a given level of permission in the conversation.
    /// </summary>
    public class RequireConversationPermissionsAttribute : CheckBaseAttribute
    {
        public ConversationPermissions RequiredPermissions { get; private set; }

        public RequireConversationPermissionsAttribute(ConversationPermissions permissions)
        {
            RequiredPermissions = permissions;
        }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            switch (RequiredPermissions)
            {
                case ConversationPermissions.Verified:
                    return Task.FromResult(ConversationData.VerifiedUsers.Contains(ctx.User.Id));
                case ConversationPermissions.Moderator:
                    return Task.FromResult(ConversationData.Moderators.Contains(ctx.User.Id));
                case ConversationPermissions.Admin:
                    return Task.FromResult(ConversationData.Administrators.Contains(ctx.User.Id));
                case ConversationPermissions.SuperAdmin:
                    return Task.FromResult(ConversationData.SuperAdministrators.Contains(ctx.User.Id));
                case ConversationPermissions.Developer:
                    return (Task.FromResult(ctx.User.Id == Program.Mrcarrot.Id));
            }
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Checks to see if a user has a given level of permission in the conversation.
    /// </summary>
    public class SlashRequireConversationPermissionsAttribute : SlashCheckBaseAttribute
    {
        public ConversationPermissions RequiredPermissions { get; private set; }

        public SlashRequireConversationPermissionsAttribute(ConversationPermissions permissions)
        {
            RequiredPermissions = permissions;
        }

        public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            switch (RequiredPermissions)
            {
                case ConversationPermissions.Verified:
                    return Task.FromResult(ConversationData.VerifiedUsers.Contains(ctx.User.Id));
                case ConversationPermissions.Moderator:
                    return Task.FromResult(ConversationData.Moderators.Contains(ctx.User.Id));
                case ConversationPermissions.Admin:
                    return Task.FromResult(ConversationData.Administrators.Contains(ctx.User.Id));
                case ConversationPermissions.SuperAdmin:
                    return Task.FromResult(ConversationData.SuperAdministrators.Contains(ctx.User.Id));
                case ConversationPermissions.Developer:
                    return (Task.FromResult(ctx.User.Id == Program.Mrcarrot.Id));
            }
            return Task.FromResult(false);
        }
    }
    public enum ConversationPermissions
    {
        Verified,
        Moderator,
        Admin,
        SuperAdmin,
        Developer
    }
}