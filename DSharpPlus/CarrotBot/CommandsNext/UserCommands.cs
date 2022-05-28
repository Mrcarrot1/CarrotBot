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
    //[Group("user")]
    public class UserCommands : BaseCommandModule
    {
        [Command("userinfo"), Description("Gets info about a user")]
        public async Task Info(CommandContext ctx, [Description("The user in question. Leave blank to return your own info."), RemainingText] string userMention = null)
        {
            try
            {
                ulong userId = 0;
                var member = ctx.Member;
                try
                {
                    if (userMention != null)
                    {
                        member = ctx.Guild.FindMemberAsync(userMention).Result;
                        userId = member.Id;
                    }

                }
                catch
                {
                    await ctx.RespondAsync("User not found or not a member of this guild!");
                    return;
                }
                var user = ctx.User;
                if (userId != 0)
                    user = await Program.discord.ShardClients.First().Value.GetUserAsync(userId);
                Logger.Log($"User info command: processing user {user.Username}");
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
                eb.AddField("Username", $"{user.Username}#{user.Discriminator}");
                eb.AddField("Created", $"<t:{user.CreationTimestamp.ToUnixTimeSeconds()}:R> ({user.CreationTimestamp} UTC)", true);
                eb.AddField("Type", $"{type}", true);
                if (!ctx.Channel.IsPrivate)
                {
                    string nick = member.Nickname;
                    if (nick == null || nick == "")
                        nick = member.Username;
                    eb.AddField("Nickname", $"{nick}", true);
                }
                await ctx.RespondAsync(embed: eb.Build());
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString(), Logger.CBLogLevel.EXC);
            }
        }
    }
}