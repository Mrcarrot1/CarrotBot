using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
 
namespace CarrotBot.Conversation
{
    [Group("conversation"), Description("Commands for interacting with the CarrotBot Multi-Server Conversation")]
    public class ConversationCommands : BaseCommandModule
    {
        [Command("start"), RequireConversationPermissions(ConversationPermissions.SuperAdmin)]
        public async Task StartConversation(CommandContext ctx, bool loadDatabase = true)
        {
            if(ctx.User.Id != 366298290377195522) return;
            if(Program.conversation)
            {
                await ctx.RespondAsync("Conversation is already started. Use `conversation stop` to stop.");
                return;
            }
            
            if(loadDatabase) ConversationData.LoadDatabase();
            await Conversation.SendConversationMessage("The CarrotBot Multi-Server Conversation is now active!\nRemember: you must accept the terms (%conversation acceptterms) to enter!");
            Program.conversation = true;
            await ctx.RespondAsync("Started conversation.");
        }
        [Command("stop"), RequireConversationPermissions(ConversationPermissions.SuperAdmin)]
        public async Task StopConversation(CommandContext ctx)
        {
            if(ctx.User.Id != 366298290377195522) return;
            if(!Program.conversation)
            {
                await ctx.RespondAsync("Conversation is already stopped. Use `conversation start` to start.");
                return;
            }

            await Conversation.SendConversationMessage("The CarrotBot Multi-Server Conversation is no longer active.");
            Program.conversation = false;
            ConversationData.WriteDatabase();
            await ctx.RespondAsync("Stopped conversation.");
        }
        [Command("sendmessage"), RequireConversationPermissions(ConversationPermissions.Admin)]
        public async Task SendMessage(CommandContext ctx, [RemainingText]string message)
        {
            if(ctx.User.Id != 366298290377195522) return;
            await Conversation.SendConversationMessage(message);
        }
        [Command("acceptterms"), Description("Used to accept the Conversation's terms of service")]
        public async Task AcceptTerms(CommandContext ctx, bool accept = false)
        {
            if(accept)
            {
                await ctx.RespondAsync("You have accepted the terms of the CarrotBot multi-server conversation.\nBy entering the conversation, you agree to have your data read and/or used by others, who may or may not have agreed to these terms.\nMrcarrot(the creator of CarrotBot) is not responsible for the contents of the conversation or any ways in which your data may be used.\nPlease be aware that CarrotBot does not save or cache the contents of your messages locally.");
                ConversationData.AcceptedUsers.Add(ctx.User.Id);
                ConversationData.WriteDatabase();
            }
            else
            {
                await ctx.RespondAsync("You are about to accept the terms of the CarrotBot multi-server conversation.\nBy entering the conversation, you agree to have your data read and/or used by others, who may or may not have agreed to these terms.\nMrcarrot(the creator of CarrotBot) is not responsible for the contents of the conversation or any ways in which your data may be used.\nPlease be aware that CarrotBot does not save or cache the contents of your messages locally.");
                await ctx.RespondAsync("Type `%conversation acceptterms true` to accept.");
            }
        }
        [Command("reload"), RequireConversationPermissions(ConversationPermissions.Developer)]
        public async Task ReloadDatabase(CommandContext ctx)
        {
            ConversationData.LoadDatabase();
            await ctx.RespondAsync("Reloaded conversation database.");
        }
        [Command("flushdatabase"), RequireConversationPermissions(ConversationPermissions.Developer)]
        public async Task FlushDatabase(CommandContext ctx)
        {
            ConversationData.WriteDatabase();
            await ctx.RespondAsync("Wrote conversation database to disk.");
        }
        [Command("addchannel"), Description("Used to add your channel to the conversation"), RequireUserPermissions(Permissions.ManageChannels)]
        public async Task AddChannel(CommandContext ctx, [Description("The channel to connect to the conversation")]string channel, [RemainingText, Description("What the conversation should call your server")]string name)
        {
            try
            {
                if(name == null || name == "")
                {
                    name = ctx.Guild.Name;
                }
                ulong Id = Utils.GetId(channel);
                if(ConversationData.Administrators.Contains(ctx.User.Id))
                {
                    ConversationData.ConversationChannels.Add(new ConversationChannel(Id, name, ctx.Guild.Id));
                    ConversationData.WriteDatabase();
                    await ctx.RespondAsync("Channel added to conversation.");
                    await ctx.Guild.Channels[Id].SendMessageAsync("This channel has just been added to the CarrotBot Multi-Server Conversation.\nHave fun chatting with other users!\nNote: for legal reasons, you must accept the conversation's terms(`%conversation acceptterms` to enter.");
                }
                else
                {
                    await Program.BotGuild.Channels[818960822151544873].SendMessageAsync($"Channel requested for addition to conversation by {ctx.User.Username}#{ctx.User.Discriminator}: {Id}, {name}, Guild ID: {ctx.Guild.Id}");
                    await ctx.RespondAsync("Channel submitted for review. Please be patient as you wait for the channel to be connected to the conversation.");
                }
            }
            catch
            {
                await ctx.RespondAsync($"Something went wrong. Please ensure that you are using the channel hashtag or ID and that the channel exists in this server.\nIf you are adding a channel in another server, please use `{Data.Database.GetOrCreateGuildData(ctx.Guild.Id).GuildPrefix}conversation addchannel <server Id> <channel Id> <server name>`.");
            }
        }
        [Command("addchannel"), Description("Used to add your channel to the conversation"), RequireConversationPermissions(ConversationPermissions.Admin)]
        public async Task AddChannel(CommandContext ctx, [Description("The guild the channel is in.")]ulong guildId, [Description("The channel to connect to the conversation")]string channel, [RemainingText, Description("What the conversation should call your server")]string name)
        {
            try
            {
                if(name == null || name == "")
                {
                    name = ctx.Guild.Name;
                }
                ulong Id = Utils.GetId(channel);
                if(ConversationData.Administrators.Contains(ctx.User.Id))
                {
                    ConversationData.ConversationChannels.Add(new ConversationChannel(Id, name, guildId));
                    ConversationData.WriteDatabase();
                    await ctx.RespondAsync("Channel added to conversation.");
                    await Program.discord.GetShard(guildId).GetChannelAsync(Id).Result.SendMessageAsync("This channel has just been added to the CarrotBot Multi-Server Conversation.\nHave fun chatting with other users!\nNote: for legal reasons, you must accept the conversation's terms(`%conversation acceptterms` to enter.");
                }
                else
                {
                    await Program.discord.GetShard(guildId).GetChannelAsync(818960822151544873).Result.SendMessageAsync($"Channel requested for addition to conversation by {ctx.User.Username}#{ctx.User.Discriminator}: {Id}, {name}");
                    await ctx.RespondAsync("Channel submitted for review. Please be patient as you wait for the channel to be connected to the conversation.");
                }
            }
            catch
            {
                await ctx.RespondAsync("Something went wrong. Please ensure that you are using the channel hashtag or ID and that the channel exists in this server.");
            }
        }
        [Command("removechannel"), Description("Used to remove a channel from the conversation."), RequireConversationPermissions(ConversationPermissions.Admin)]
        public async Task RemoveChannel(CommandContext ctx, string channel)
        {
            ulong Id = Utils.GetId(channel);
            ConversationData.ConversationChannels.RemoveAll(x => x.Id == Id || x.GuildId == Id);
            ConversationData.WriteDatabase();
            await ctx.RespondAsync("Removed channel from conversation.");
        }
        [Command("ban"), Description("Bans a user from being able to take part in the conversation."), RequireConversationPermissions(ConversationPermissions.Moderator)]
        public async Task BanUser(CommandContext ctx, string user)
        {
            ulong Id = Utils.GetId(user);
            if(!ConversationData.Moderators.Contains(Id))
            {
                ConversationData.BannedUsers.Add(Id);
                ConversationData.WriteDatabase();
                foreach(ConversationMessage msg in ConversationData.ConversationMessages.Values.Where(x => x.originalMessage.Author.Id == Id))
                {
                    await msg.DeleteMessage();
                }
                await ctx.RespondAsync("User banned.");
            }
            else
            {
                await ctx.RespondAsync("Cannot ban moderators or admins!");
            }
        }
        [Command("deletemsg"), Description("Deletes a message from the conversation."), RequireConversationPermissions(ConversationPermissions.Moderator)]
        public async Task DeleteMessage(CommandContext ctx, ulong msgId)
        {
            await ConversationData.ConversationMessages[msgId].DeleteMessage();
            await ctx.RespondAsync("Message deleted.");
        }
        [Command("addmod"), Description("Adds a user as a conversation moderator."), RequireConversationPermissions(ConversationPermissions.Admin)]
        public async Task AddMod(CommandContext ctx, string user, bool confirm = false)
        {
            ulong Id = Utils.GetId(user);
            DiscordMember duser = await Program.BotGuild.GetMemberAsync(Id);
            if(!confirm)
            {
                await ctx.RespondAsync($"About to add {duser.Username}#{duser.Discriminator} as a conversation moderator.\nType `{Data.Database.GetOrCreateGuildData(ctx.Guild.Id).GuildPrefix}conversation addmod {Id} true` to continue.");
            }
            else
            {
                ConversationData.Moderators.Add(Id);
                ConversationData.WriteDatabase();
                await ctx.RespondAsync($"Added {duser.Username} as a conversation moderator.");
            }
        }
        [Command("removemod"), Description("Removes a user from being a conversation moderator."), RequireConversationPermissions(ConversationPermissions.Admin)]
        public async Task RemoveMod(CommandContext ctx, string user, bool confirm = false)
        {
            ulong Id = Utils.GetId(user);
            DiscordMember duser = await Program.BotGuild.GetMemberAsync(Id);
            if(!confirm)
            {
                await ctx.RespondAsync($"About to remove {duser.Username}#{duser.Discriminator} from being a conversation moderator.\nType `{Data.Database.GetOrCreateGuildData(ctx.Guild.Id).GuildPrefix}conversation addmod {Id} true` to continue.");
            }
            else
            {
                ConversationData.Moderators.Remove(Id);
                ConversationData.WriteDatabase();
                await ctx.RespondAsync($"Removed {duser.Username} as a conversation moderator.");
            }
        }
        [Command("approveuser"), Description("Adds a user to the conversation verified list."), RequireConversationPermissions(ConversationPermissions.Moderator)]
        public async Task ApproveUser(CommandContext ctx, string user)
        {
            ulong Id = Utils.GetId(user);
            DiscordUser duser = await ctx.Guild.GetMemberAsync(Id);
            ConversationData.VerifiedUsers.Add(Id);
            ConversationData.WriteDatabase();
            await ctx.RespondAsync($"Added {duser.Username} as a verified conversation user.");
        }
    }
}