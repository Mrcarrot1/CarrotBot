using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace CarrotBot.SlashCommands;

[SlashCommandGroup("server", "Commands for interacting with a given server.")]
public class ServerCommands : ApplicationCommandModule
{
    [SlashCommand("owner", "Shows information about the server owner."), SlashRequireGuild]
    public async Task Owner(InteractionContext ctx)
    {
        await ctx.IndicateResponseAsync();
        var eb = new DiscordEmbedBuilder();
        eb.WithDescription($"<@{ctx.Guild.Owner.Id}>\n{ctx.Guild.Owner.Username}\n{ctx.Guild.Owner.Id}\n{ctx.Guild.Name}");
        eb.Color = Utils.CBGreen;
        //eb.WithFooter("© Mrcarrot 2018-21. All Rights Reserved.");
        eb.WithThumbnail(ctx.Guild.Owner.GetAvatarUrl(ImageFormat.Auto));
        eb.WithTitle("Server Owner");
        await ctx.UpdateResponseAsync(embed: eb.Build());
    }
    [SlashCommand("info", "Shows information about the server."), SlashRequireGuild]
    public async Task Info(InteractionContext ctx)
    {
        await ctx.IndicateResponseAsync();
        var eb = new DiscordEmbedBuilder();
        eb.Color = Utils.CBGreen;
        //eb.WithFooter("© Mrcarrot 2018-21. All Rights Reserved.");
        eb.WithThumbnail(ctx.Guild.IconUrl);
        eb.WithTitle("Server Info");
        //eb.WithDescription($"Name: {ctx.Guild.Name}\nOwner: <@{ctx.Guild.Owner.Id}>\nCreated at: ({ctx.Guild.CreationTimestamp.ToUniversalTime().ToString()} UTC)\nChannels: {ctx.Guild.Channels.Count}");

        eb.AddField("Name", $"{ctx.Guild.Name}");
        eb.AddField("Owned By", $"<@!{ctx.Guild.Owner.Id}>", true);
        eb.AddField("Created At", $"<t:{ctx.Guild.CreationTimestamp.ToUnixTimeSeconds()}:R> ({ctx.Guild.CreationTimestamp.ToUniversalTime().ToString("yyyy/MM/dd HH:mm:ss")} UTC)");
        int textChannels = 0;
        int voiceChannels = 0;
        int categories = 0;
        foreach (DiscordChannel channel in ctx.Guild.Channels.Values)
        {
            if (channel.Type is ChannelType.Text or ChannelType.News) textChannels++;
            if (channel.Type == ChannelType.Voice) voiceChannels++;
            if (channel.Type == ChannelType.Category) categories++;
        }
        eb.AddField("Text Channels", $"{textChannels}", true);
        eb.AddField("Voice Channels", $"{voiceChannels}", true);
        eb.AddField("Total Channels", $"{ctx.Guild.Channels.Count - categories}", true);
        await ctx.UpdateResponseAsync(embed: eb.Build());
    }
}