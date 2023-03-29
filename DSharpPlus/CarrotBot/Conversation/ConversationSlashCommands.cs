using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace CarrotBot.Conversation;

[SlashCommandGroup("conversation", "Commands for interacting with the CarrotBot Multi-Server Conversation.")]
public class ConversationSlashCommands : ApplicationCommandModule
{
    [SlashCommand("accept-terms", "Used to accept the Conversation's terms of service")]
    public async Task AcceptTerms(InteractionContext ctx, [Option("accept", "Whether or not to confirm that you are accepting the terms.")] bool accept = false)
    {
        await ctx.IndicateResponseAsync();
        if (accept)
        {
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle("Terms Accepted");
            eb.WithDescription("You have accepted the terms of the CarrotBot multi-server conversation.\nBy entering the conversation, you agree to have your message data read and/or used by others, who may or may not have agreed to these terms.\nMrcarrot(the creator of CarrotBot) is not responsible for the contents of the conversation or any ways in which your data may be used.\nMessages sent in the conversation will be logged on Discord for conversation moderators and administrators but not stored locally. Messages sent outside the conversation will not.\nPlease be aware that CarrotBot does not save or cache the contents of your messages locally.\nAdditionally, messages outside of the conversation will not be shared.\nTo opt out of these terms in future, use `conversation optout`.");
            eb.WithColor(Utils.CBGreen);
            await ctx.UpdateResponseAsync(embed: eb.Build());
            ConversationData.AcceptedUsers.Add(ctx.User.Id);
            ConversationData.WriteDatabase();
        }
        else
        {
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle("Conversation Terms");
            eb.WithDescription("You are about to accept the terms of the CarrotBot multi-server conversation.\nBy entering the conversation, you agree to have your message data read and/or used by others, who may or may not have agreed to these terms.\nMrcarrot(the creator of CarrotBot) is not responsible for the contents of the conversation or any ways in which your data may be used.\nMessages sent in the conversation will be logged on Discord for conversation moderators and administrators but not stored locally. Messages sent outside the conversation will not.\nPlease be aware that CarrotBot does not save or cache the contents of your messages locally.\nAdditionally, messages outside of the conversation will not be shared.\nTo accept these terms, use `conversation acceptterms true`.");
            eb.WithColor(Utils.CBOrange);
            await ctx.UpdateResponseAsync(embed: eb.Build());
        }
    }

    [SlashCommand("opt-out", "Used to opt out of the Conversation's terms of service.")]
    public async Task OptOut(InteractionContext ctx)
    {
        await ctx.IndicateResponseAsync();
        if (ConversationData.AcceptedUsers.Contains(ctx.User.Id))
        {
            ConversationData.AcceptedUsers.RemoveAll(x => x == ctx.User.Id);
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle("Opted Out");
            eb.WithDescription("You have successfully opted out from the CarrotBot Multi-Server Conversation Terms of Service.\nFrom this point forward, no message data will be stored or shared.");
            eb.WithColor(Utils.CBGreen);
            await ctx.UpdateResponseAsync(eb.Build());
        }
    }

    [SlashCommand("add-channel", "Used to add your channel to the conversation", false), SlashRequireUserPermissions(Permissions.ManageChannels)]
    public async Task AddChannel(InteractionContext ctx, [Option("channel", "The channel to connect to the conversation.")] DiscordChannel channel, [Option("name", "What the conversation should call your server.")] string name)
    {
        await ctx.IndicateResponseAsync();
        try
        {
            if (name == "")
            {
                name = ctx.Guild.Name;
            }
            ulong Id = channel.Id;
            if (ConversationData.Administrators.Contains(ctx.User.Id))
            {
                ConversationData.ConversationChannels.Add(new ConversationChannel(Id, name, ctx.Guild.Id));
                ConversationData.WriteDatabase();
                await ctx.UpdateResponseAsync("Channel added to conversation.");
                await ctx.Guild.Channels[Id].SendMessageAsync("This channel has just been added to the CarrotBot Multi-Server Conversation.\nHave fun chatting with other users!\nNote: for legal reasons, you must accept the conversation's terms(`%conversation acceptterms` to enter.");
            }
            else
            {
                await Program.BotGuild!.Channels[818960822151544873].SendMessageAsync($"Channel requested for addition to conversation by {ctx.User.Username}#{ctx.User.Discriminator}: {Id}, {name}, Guild ID: {ctx.Guild.Id}");
                await ctx.UpdateResponseAsync("Channel submitted for review. Please be patient as you wait for the channel to be connected to the conversation.");
            }
        }
        catch
        {
            await ctx.UpdateResponseAsync("Something went wrong. Please ensure that you are using a valid channel in this server.");
        }
    }

}