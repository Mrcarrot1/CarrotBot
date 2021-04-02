using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace CarrotBot.Commands
{
    [Group("server"), Description("Commands for interacting with a given server"), Aliases("guild")]
    public class ServerCommands
    {
        [Command("owner")]
        public async Task Owner(CommandContext ctx)
        {
            var eb = new DiscordEmbedBuilder();
            eb.WithDescription($"<@{ctx.Guild.Owner.Id}>\n{ctx.Guild.Owner.Username}#{ctx.Guild.Owner.Discriminator}\n{ctx.Guild.Owner.Id}\n{ctx.Guild.Name}");
            eb.Color = DiscordColor.Green;
            eb.WithFooter("© Mrcarrot 2018-21. All Rights Reserved.");
            eb.WithThumbnailUrl(ctx.Guild.Owner.AvatarUrl);
            eb.WithTitle("Server Owner");
            await ctx.RespondAsync(embed: eb.Build());
        }
        [Command("info")]
        public async Task Info(CommandContext ctx)
        {
            var eb = new DiscordEmbedBuilder();
            eb.Color = DiscordColor.Green;
            eb.WithFooter("© Mrcarrot 2018-21. All Rights Reserved.");
            eb.WithThumbnailUrl(ctx.Guild.IconUrl);
            eb.WithTitle("Server Info");
            eb.WithDescription($"Name: {ctx.Guild.Name}\nOwner: <@{ctx.Guild.Owner.Id}>\nVoice Region: {ctx.Guild.GetVoiceRegionsAsync().Result[0].Name}\nCreated at: {ctx.Guild.CreationTimestamp.ToUniversalTime().ToString()} (UTC)\nChannels: {ctx.Guild.Channels.Count}");
            await ctx.RespondAsync(embed: eb.Build());
        }
    }
}