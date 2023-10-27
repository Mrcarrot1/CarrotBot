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
//[Group("user")]
public class UserCommands : ApplicationCommandModule
{
    [SlashCommand("user-info", "Gets info about a user.")]
    public async Task Info(InteractionContext ctx, [Option("user", "The user in question. Leave blank to return your own info.")] DiscordUser user = null)
    {
        await ctx.IndicateResponseAsync();
        try
        {
            ulong userId = 0;
            var member = ctx.Member;
            try
            {
                if (user != null)
                {
                    member = await ctx.Guild.GetMemberAsync(user.Id);
                    userId = member.Id;
                }
            }
            catch
            {
                await ctx.RespondAsync("User not found or not a member of this guild!");
                return;
            }
            if (user == null) user = ctx.User;
            if (userId != 0)
                user = await Program.discord.ShardClients.First().Value.GetUserAsync(userId);
            string type = "User";
            if (user.IsBot)
                type = "Bot";
            /*if (user.Id == 366298290377195522)
                type = "**Robot Overlord**";
            if (user.Id == Program.discord.CurrentUser.Id)
                type = "Yours truly";*/
            var eb = new DiscordEmbedBuilder();
            eb.WithColor(Utils.CBGreen);

            //eb.WithFooter("© Mrcarrot 2018-21. All Rights Reserved.");
            eb.WithThumbnail(user.AvatarUrl);
            eb.WithTitle("User Info");
            //eb.WithDescription($"{user.Username + "#" + user.Discriminator}\nNickname: {nick}\nCreated At: {user.CreationTimestamp} (UTC)\nType: {type}\nStatus: {status}");
            eb.AddField("Username", $"{user.Username}");
            eb.AddField("Created", $"<t:{user.CreationTimestamp.ToUnixTimeSeconds()}:R> ({user.CreationTimestamp.ToUniversalTime().ToString("yyyy/MM/dd HH:mm:ss")} UTC)", true);
            eb.AddField("Type", $"{type}", true);
            if (!ctx.Channel.IsPrivate)
            {
                string nick = member.Nickname;
                if (nick == null || nick == "")
                    nick = member.Username;
                eb.AddField("Nickname", $"{nick}", true);
            }
            await ctx.UpdateResponseAsync(embed: eb.Build());
        }
        catch (Exception e)
        {
            await ctx.UpdateResponseAsync("Something went wrong.");
            Logger.Log(e.ToString(), Logger.CBLogLevel.EXC);
        }
    }
}