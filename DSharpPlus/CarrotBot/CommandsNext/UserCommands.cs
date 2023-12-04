using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace CarrotBot.CommandsNext
{
    //[Group("user")]
    public class UserCommands : BaseCommandModule
    {
        [Command("userinfo"), Description("Gets info about a user")]
        public async Task Info(CommandContext ctx, [Description("The user in question. Leave blank to return your own info."), RemainingText] DiscordUser? user = null)
        {
            try
            {
                user ??= ctx.User;
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
                eb.AddField("Username", $"{user.Username}");
                eb.AddField("Created", $"<t:{user.CreationTimestamp.ToUnixTimeSeconds()}:R> ({user.CreationTimestamp} UTC)", true);
                eb.AddField("Type", $"{type}", true);
                if (!ctx.Channel.IsPrivate)
                {
                    DiscordMember? member = user as DiscordMember;
                    string nick = member!.Nickname;
                    if (string.IsNullOrEmpty(nick))
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