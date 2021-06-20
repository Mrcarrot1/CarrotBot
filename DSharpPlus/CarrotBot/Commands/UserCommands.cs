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
    public class UserCommands
    {
        [Command("userinfo"), Description("Gets info about a user")]
        public async Task Info(CommandContext ctx, [Description("The user in question. Leave blank to return your own info.")]string userMention = null)
        {
            try {
            ulong userId = 0;
            try
            {
                if (userMention != null)
                    userId = Utils.GetId(userMention);
            }
            catch(FormatException)
            {

            }
            var member = ctx.Member;
            try
            {
                if (userId != 0)
                    member = ctx.Guild.GetMemberAsync(userId).Result;
            }
            catch
            {
                await ctx.RespondAsync("User not found or not a member of this guild!");
                return;
            }
            var user = member as DiscordUser;
            Logger.Log($"User info command: processing user {user.Username}");
            string type = "User";
            if (user.IsBot)
                type = "Bot";
            if (user.Id == 366298290377195522)
                type = "**Robot Overlord**";
            if (user.Id == Program.discord.CurrentUser.Id)
                type = "Yours truly";
            var eb = new DiscordEmbedBuilder();
            /*if (member.Presence.Status == UserStatus.Online)
                eb.Color = DiscordColor.Green;
            if (member.Presence.Status == UserStatus.Idle)
                eb.Color = DiscordColor.Gold;
            if (member.Presence.Status == UserStatus.DoNotDisturb)
                eb.Color = new DiscordColor(255, 0, 0);
            if (member.Presence.Status == UserStatus.Offline || user.Presence.Status == UserStatus.Invisible)
                eb.Color = DiscordColor.DarkGray;
            string status = member.Presence.Status.ToString();
            if (status == "DoNotDisturb")
                status = "Do Not Disturb";*/
            string nick = member.Nickname;
            if(nick == null || nick == "")
                nick = member.Username;
            eb.WithFooter("© Mrcarrot 2018-21. All Rights Reserved.");
            eb.WithThumbnailUrl(user.AvatarUrl);
            eb.WithTitle("User Info");
            eb.WithDescription($"{user.Username + "#" + user.Discriminator}\nNickname: {nick}\nCreated At: {user.CreationTimestamp} (UTC)\nType: {type}");
            await ctx.RespondAsync(embed: eb.Build());
            }
            catch(Exception e)
            {
                Logger.Log(e.ToString());
            }
        }
    }
}