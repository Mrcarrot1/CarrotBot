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
    [Group("bot"), Description("Commands for various CarrotBot functions")]
    public class BotCommands : BaseCommandModule
    {
        [Command("server"), Description("Provides the link to the CarrotBot Discord server")]
        public async Task BotServer(CommandContext ctx)
        {
            await ctx.RespondAsync("Join the server for CarrotBot support and testing at:\nhttps://discord.gg/wHPwHu7");
        }
        [Command("invite"), Description("Provides the link to invite CarrotBot to a server")]
        public async Task BotInvite(CommandContext ctx)
        {
            await ctx.RespondAsync("Invite CarrotBot to your server at:\nhttps://top.gg/bot/389513870835974146");
        }
        [Command("reportbug"), Description("Reports a bug with the bot")]
        public async Task ReportBug(CommandContext ctx, [RemainingText, Description("A description of the bug you are experiencing.")]string bug)
        {
            await Program.Mrcarrot.SendMessageAsync($"Bug reported by {ctx.User.Username}#{ctx.User.Discriminator}: {bug}");
            await ctx.RespondAsync("Bug reported.");
        }
        [Command("suggestfeature"), Description("Suggests a feature for the bot")]
        public async Task SuggestFeature(CommandContext ctx, [RemainingText, Description("A description of the feature you would like to suggest.")]string feature)
        {
            await Program.Mrcarrot.SendMessageAsync($"Feature suggested by {ctx.User.Username}#{ctx.User.Discriminator}: {feature}");
            await ctx.RespondAsync("Feature suggested.");
        }
    }
}