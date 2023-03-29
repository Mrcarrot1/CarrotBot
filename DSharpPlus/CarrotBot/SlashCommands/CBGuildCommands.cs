using System.Linq;
using System.Threading.Tasks;
using CarrotBot.Conversation;
using CarrotBot.Data;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace CarrotBot.SlashCommands;

public class CBGuildCommands : ApplicationCommandModule
{
    [SlashCommand("update-ping", "Grants or revokes the Update Ping role in the CarrotBot server."), SlashRequireGuild]
    public async Task UpdatePing(InteractionContext ctx)
    {
        await ctx.IndicateResponseAsync();
        if (!ctx.Guild.Equals(Program.BotGuild)) return;
        DiscordRole role = ctx.Guild.Roles.FirstOrDefault(x => x.Value.Name == "Updoot Ping").Value;
        if (!ctx.Member.Roles.ToList().Contains(role))
        {
            await ctx.Member.GrantRoleAsync(role, "Given by user request");
            await ctx.UpdateResponseAsync("Role granted.");
        }
        else
        {
            await ctx.Member.RevokeRoleAsync(role, "Revoked by user request");
            await ctx.UpdateResponseAsync("Role removed.");
        }
    }
    [SlashCommand("conv-start", "Starts the conversation.", false), SlashRequireConversationPermissions(ConversationPermissions.SuperAdmin)]
    public async Task StartConversation(InteractionContext ctx, [Option("load-database", "Whether or not to (re)load the conversation database.")] bool loadDatabase = true)
    {
        await ctx.IndicateResponseAsync();
        if (ctx.User.Id != 366298290377195522) return;
        if (Program.conversation)
        {
            await ctx.UpdateResponseAsync("Conversation is already started. Use `conversation stop` to stop.");
            return;
        }

        if (loadDatabase) ConversationData.LoadDatabase();
        await Conversation.Conversation.SendConversationMessage("The CarrotBot Multi-Server Conversation is now active!\nRemember: you must accept the terms (%conversation acceptterms) to enter!");
        Program.conversation = true;
        await ctx.UpdateResponseAsync("Started conversation.");
    }
    [SlashCommand("conv-stop", "Stops the conversation.", false), SlashRequireConversationPermissions(ConversationPermissions.SuperAdmin)]
    public async Task StopConversation(InteractionContext ctx)
    {
        await ctx.IndicateResponseAsync();
        if (ctx.User.Id != 366298290377195522) return;
        if (!Program.conversation)
        {
            await ctx.UpdateResponseAsync("Conversation is already stopped. Use `conversation start` to start.");
            return;
        }

        await Conversation.Conversation.SendConversationMessage("The CarrotBot Multi-Server Conversation is no longer active.");
        Program.conversation = false;
        ConversationData.WriteDatabase();
        await ctx.UpdateResponseAsync("Stopped conversation.");
    }
    [SlashCommand("conv-sendmessage", "Sends a message to all conversation channels.", false), SlashRequireConversationPermissions(ConversationPermissions.Admin)]
    public async Task SendMessage(InteractionContext ctx, [Option("message", "The message to send.")] string message)
    {
        await ctx.IndicateResponseAsync();
        if (ctx.User.Id != 366298290377195522) return;
        await Conversation.Conversation.SendConversationMessage(message);
        await ctx.UpdateResponseAsync("Message sent.");
    }

    [SlashCommand("conv-reload", "Reloads the conversation.", false), SlashRequireConversationPermissions(ConversationPermissions.Developer)]
    public async Task ReloadDatabase(InteractionContext ctx)
    {
        await ctx.IndicateResponseAsync();
        ConversationData.LoadDatabase();
        await ctx.UpdateResponseAsync("Reloaded conversation database.");
    }
    [SlashCommand("conv-flushdatabase", "Writes all conversation data to disk.", false), SlashRequireConversationPermissions(ConversationPermissions.Developer)]
    public async Task FlushDatabase(InteractionContext ctx)
    {
        await ctx.IndicateResponseAsync();
        ConversationData.WriteDatabase();
        await ctx.UpdateResponseAsync("Wrote conversation database to disk.");
    }

    [SlashCommand("conv-addchannelwgid", "Used to add your channel to the conversation.", false), SlashRequireConversationPermissions(ConversationPermissions.Admin)]
    public async Task AddChannel(InteractionContext ctx, [Option("guild-id", "The guild the channel is in.")] long guildIds, [Option("channel", "The channel to connect to the conversation")] string? channel, [Option("name", "What the conversation should call your server")] string name)
    {
        await ctx.IndicateResponseAsync();
        try
        {
            if (name == "")
            {
                name = ctx.Guild.Name;
            }
            ulong guildId = (ulong)guildIds;
            ulong Id = Utils.GetId(channel);
            if (ConversationData.Administrators.Contains(ctx.User.Id))
            {
                ConversationData.ConversationChannels.Add(new ConversationChannel(Id, name, guildId));
                ConversationData.WriteDatabase();
                await ctx.UpdateResponseAsync("Channel added to conversation.");
                DiscordChannel discordChannel = await Program.discord!.GetShard(guildId).GetChannelAsync(Id);
                await discordChannel.SendMessageAsync("This channel has just been added to the CarrotBot Multi-Server Conversation.\nHave fun chatting with other users!\nNote: for legal reasons, you must accept the conversation's terms(`%conversation acceptterms` to enter.");
            }
            else
            {
                DiscordChannel discordChannel = await Program.discord!.GetShard(guildId).GetChannelAsync(818960822151544873);
                await discordChannel.SendMessageAsync($"Channel requested for addition to conversation by {ctx.User.Username}#{ctx.User.Discriminator}: {Id}, {name}");
                await ctx.UpdateResponseAsync("Channel submitted for review. Please be patient as you wait for the channel to be connected to the conversation.");
            }
        }
        catch
        {
            await ctx.UpdateResponseAsync("Something went wrong. Please ensure that you are using the channel hashtag or ID and that the channel exists in this server.");
        }
    }
    [SlashCommand("conv-removechannel", "Used to remove a channel from the conversation.", false), SlashRequireConversationPermissions(ConversationPermissions.Admin)]
    public async Task RemoveChannel(InteractionContext ctx, [Option("channel", "The channel to remove.")] string? channel)
    {
        await ctx.IndicateResponseAsync();
        ulong Id = Utils.GetId(channel);
        ConversationData.ConversationChannels.RemoveAll(x => x.Id == Id || x.GuildId == Id);
        ConversationData.WriteDatabase();
        await ctx.UpdateResponseAsync("Removed channel from conversation.");
    }
    [SlashCommand("conv-ban", "Bans a user from being able to take part in the conversation.", false), SlashRequireConversationPermissions(ConversationPermissions.Moderator)]
    public async Task BanUser(InteractionContext ctx, [Option("user", "The user to ban.")] DiscordUser user)
    {
        await ctx.IndicateResponseAsync();
        ulong Id = user.Id;
        if (!ConversationData.Moderators.Contains(Id))
        {
            ConversationData.BannedUsers.Add(Id);
            ConversationData.WriteDatabase();
            foreach (ConversationMessage msg in ConversationData.ConversationMessages.Values.Where(x => x.originalMessage.Author.Id == Id))
            {
                await msg.DeleteMessage();
            }
            await ctx.UpdateResponseAsync("User banned.");
        }
        else
        {
            await ctx.UpdateResponseAsync("Cannot ban moderators or admins!");
        }
    }
    [SlashCommand("conv-deletemsg", "Deletes a message from the conversation.", false), SlashRequireConversationPermissions(ConversationPermissions.Moderator)]
    public async Task DeleteMessage(InteractionContext ctx, [Option("msg-id", "The ID of the message.")] long msgIdl)
    {
        await ctx.IndicateResponseAsync();
        ulong msgId = (ulong)msgIdl;
        await ConversationData.ConversationMessages[msgId].DeleteMessage();
        await ctx.UpdateResponseAsync("Message deleted.");
    }
    [SlashCommand("conv-addmod", "Adds a user as a conversation moderator.", false), SlashRequireConversationPermissions(ConversationPermissions.Admin)]
    public async Task AddMod(InteractionContext ctx, [Option("user", "The user to add.")] DiscordUser user, [Option("confirm", "Whether to confirm this action.")] bool confirm = false)
    {
        await ctx.IndicateResponseAsync();
        ulong Id = user.Id;
        //DiscordMember duser = await Program.BotGuild.GetMemberAsync(Id);
        if (!confirm)
        {
            await ctx.UpdateResponseAsync($"About to add {user.Username}#{user.Discriminator} as a conversation moderator.\nType `{Database.GetOrCreateGuildData(ctx.Guild.Id).GuildPrefix}conversation addmod {Id} true` to continue.");
        }
        else
        {
            ConversationData.Moderators.Add(Id);
            ConversationData.WriteDatabase();
            await ctx.UpdateResponseAsync($"Added {user.Username} as a conversation moderator.");
        }
    }
    [SlashCommand("conv-removemod", "Removes a user from being a conversation moderator.", false), SlashRequireConversationPermissions(ConversationPermissions.Admin)]
    public async Task RemoveMod(InteractionContext ctx, [Option("user", "The user to remove.")] DiscordUser user, [Option("confirm", "Whether to confirm this action.")] bool confirm = false)
    {
        await ctx.IndicateResponseAsync();
        ulong Id = user.Id;
        //DiscordMember duser = await Program.BotGuild.GetMemberAsync(Id);
        if (!confirm)
        {
            await ctx.UpdateResponseAsync($"About to remove {user.Username}#{user.Discriminator} from being a conversation moderator.\nType `{Database.GetOrCreateGuildData(ctx.Guild.Id).GuildPrefix}conversation addmod {Id} true` to continue.");
        }
        else
        {
            ConversationData.Moderators.Remove(Id);
            ConversationData.WriteDatabase();
            await ctx.UpdateResponseAsync($"Removed {user.Username} as a conversation moderator.");
        }
    }
    [SlashCommand("conv-approveuser", "Adds a user to the conversation verified list.", false), SlashRequireConversationPermissions(ConversationPermissions.Moderator)]
    public async Task ApproveUser(InteractionContext ctx, [Option("user", "The user to approve.")] DiscordUser user)
    {
        await ctx.IndicateResponseAsync();
        ulong Id = user.Id;
        ConversationData.VerifiedUsers.Add(Id);
        ConversationData.WriteDatabase();
        await ctx.UpdateResponseAsync($"Added {user.Username} as a verified conversation user.");
    }
}