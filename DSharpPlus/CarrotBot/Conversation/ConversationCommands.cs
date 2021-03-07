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
    public class ConversationCommands
    {
        [Command("start")]
        public async Task StartConversation(CommandContext ctx, bool loadDatabase = true)
        {
            if(ctx.User.Id != 366298290377195522) return;
            if(Program.conversation)
            {
                await ctx.RespondAsync("Conversation is already started. Use `conversation stop` to stop.");
                return;
            }
            
            if(loadDatabase) Conversation.LoadDatabase();
            await Conversation.SendConversationMessage("WARNING: THIS IS A HIGHLY UNSTABLE BETA\nThe CarrotBot Multi-Server Conversation is now active!\nRemember: you must accept the terms (%conversation acceptterms) to enter!");
            Program.conversation = true;
            await ctx.RespondAsync("Started conversation.");
        }
        [Command("stop")]
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
            await ctx.RespondAsync("Stopped conversation.");
        }
        [Command("sendmessage")]
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
                await ctx.RespondAsync("You have accepted the terms of the CarrotBot multi-server conversation.\nBy entering the conversation, you agree to have your data read and/or used by others, who may or may not have agreed to these terms.\nMrcarrot(the creator of CarrotBot) is not responsible for the contents of the conversation or any ways in which your data may be used.");
                Conversation.AcceptedUsers.Add(ctx.User.Id);
                File.AppendAllText($@"{Utils.localDataPath}/AcceptedUsers.cb", $",{ctx.User.Id}");
            }
            else
            {
                await ctx.RespondAsync("You are about to accept the terms of the CarrotBot multi-server conversation.\nBy entering the conversation, you agree to have your data read and/or used by others, who may or may not have agreed to these terms.\nMrcarrot(the creator of CarrotBot) is not responsible for the contents of the conversation or any ways in which your data may be used.");
                await ctx.RespondAsync("Type `%conversation acceptterms true` to accept.");
            }
        }
        [Command("reload")]
        public async Task ReloadDatabase(CommandContext ctx)
        {
            Conversation.LoadDatabase();
            await ctx.RespondAsync("Reloaded conversation database.");
        }
        [Command("addchannel"), Description("Used to add your channel to the conversation")]
        public async Task AddChannel(CommandContext ctx, string channel, string name)
        {
            ulong Id = Utils.GetId(channel);
            if(ctx.User.Id == 366298290377195522)
            {
                File.AppendAllText($@"{Utils.localDataPath}/ConversationServers.csv", $"\n{Id},{name}");
                Conversation.LoadDatabase();
                await ctx.RespondAsync("Channel added to conversation.");
            }
            else
            {
                await Program.Mrcarrot.SendMessageAsync($"Channel requested for addition to conversation: {Id}, {name}");
                await ctx.RespondAsync("Channel submitted for review. Please be patient as you wait for the channel to be connected to the conversation.");
            }
        }
    }
}