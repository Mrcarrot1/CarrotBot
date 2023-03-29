using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace CarrotBot.CommandsNext
{
    [Group("server"), Description("Commands for interacting with a given server"), Aliases("guild")]
    public class ServerCommands : BaseCommandModule
    {
        [Command("owner")]
        public async Task Owner(CommandContext ctx)
        {
            var eb = new DiscordEmbedBuilder();
            eb.WithDescription($"<@{ctx.Guild.Owner.Id}>\n{ctx.Guild.Owner.Username}#{ctx.Guild.Owner.Discriminator}\n{ctx.Guild.Owner.Id}\n{ctx.Guild.Name}");
            eb.Color = Utils.CBGreen;
            //eb.WithFooter("© Mrcarrot 2018-21. All Rights Reserved.");
            eb.WithThumbnail(ctx.Guild.Owner.GetAvatarUrl(ImageFormat.Auto));
            eb.WithTitle("Server Owner");
            await ctx.RespondAsync(embed: eb.Build());
        }
        [Command("info")]
        public async Task Info(CommandContext ctx)
        {
            if (ctx.Channel.IsPrivate)
            {
                await ctx.RespondAsync("You need to be in a server to use this command!");
                return;
            }
            DiscordEmbedBuilder eb = new()
            {
                Color = Utils.CBGreen
            };
            //eb.WithFooter("© Mrcarrot 2018-21. All Rights Reserved.");
            eb.WithThumbnail(ctx.Guild.IconUrl);
            eb.WithTitle("Server Info");
            //eb.WithDescription($"Name: {ctx.Guild.Name}\nOwner: <@{ctx.Guild.Owner.Id}>\nCreated at: ({ctx.Guild.CreationTimestamp.ToUniversalTime().ToString()} UTC)\nChannels: {ctx.Guild.Channels.Count}");

            eb.AddField("Name", $"{ctx.Guild.Name}");
            eb.AddField("Owned By", $"<@!{ctx.Guild.Owner.Id}>", true);
            eb.AddField("Created At", $"<t:{ctx.Guild.CreationTimestamp.ToUnixTimeSeconds()}:R> ({ctx.Guild.CreationTimestamp.ToUniversalTime()} UTC)");
            eb.AddField("Voice Region", $"{ctx.Guild.VoiceRegion.Name}");
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
            await ctx.RespondAsync(embed: eb.Build());
        }
    }
}