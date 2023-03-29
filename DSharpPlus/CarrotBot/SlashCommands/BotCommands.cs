using System.Threading.Tasks;
using DSharpPlus.SlashCommands;

namespace CarrotBot.SlashCommands;

[SlashCommandGroup("bot", "Commands for various CarrotBot-related functions")]
public class BotCommands : ApplicationCommandModule
{
    [SlashCommand("server", "Provides the link to the CarrotBot Discord server")]
    public async Task BotServer(InteractionContext ctx)
    {
        await ctx.RespondAsync("Join the server for CarrotBot support and testing at:\nhttps://discord.gg/wHPwHu7");
    }
    [SlashCommand("invite", "Provides the link to invite CarrotBot to a server")]
    public async Task BotInvite(InteractionContext ctx)
    {
        await ctx.RespondAsync("Invite CarrotBot to your server at:\nhttps://discord.bots.gg/bots/389513870835974146");
    }
    [SlashCommand("report-bug", "Reports a bug with the bot")]
    public async Task ReportBug(InteractionContext ctx, [Option("bug", "A description of the bug you are experiencing.")] string bug)
    {
        await ctx.IndicateResponseAsync();
        await Program.Mrcarrot!.SendMessageAsync($"Bug reported by {ctx.User.Username}#{ctx.User.Discriminator}: {bug}");
        await ctx.UpdateResponseAsync("Bug reported.");
    }
    [SlashCommand("suggest-feature", "Suggests a feature for the bot.")]
    public async Task SuggestFeature(InteractionContext ctx, [Option("feature", "A description of the feature you would like to suggest.")] string feature)
    {
        await ctx.IndicateResponseAsync();
        await Program.Mrcarrot!.SendMessageAsync($"Feature suggested by {ctx.User.Username}#{ctx.User.Discriminator}: {feature}");
        await ctx.UpdateResponseAsync("Feature suggested.");
    }
    //[Command("remoteupdate"), RequireOwner]
    /*public async Task RemoteUpdate(CommandContext ctx, string fileUrl = null)
    {
        if(fileUrl == null)
            fileUrl = ctx.Message.Attachments.First().Url;
        HttpClient client = new HttpClient();
        await client.DownloadFileTaskAsync(new Uri(fileUrl), $@"{Utils.localDataPath}/Update.zip");
        string updatesPath = Directory.GetParent(Directory.GetParent(Environment.CurrentDirectory).FullName).FullName + @"/CBUpdates";
        ZipFile.ExtractToDirectory($@"{Utils.localDataPath}/Update.zip", updatesPath);
        await ctx.RespondAsync("Downloaded updates to be applied at next system restart.");
    }*/
}