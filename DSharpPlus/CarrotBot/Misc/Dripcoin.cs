using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CarrotBot.Leveling;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using KarrotObjectNotation;

namespace CarrotBot
{
    public static class Dripcoin
    {
        public static Dictionary<ulong, double> UserBalances = new Dictionary<ulong, double>();

        public static void CreateUser(ulong Id)
        {
            if (LevelingData.Servers[824824193001979924].Users.ContainsKey(Id))
            {
                // ReSharper disable once PossibleLossOfFraction
                UserBalances.Add(Id, LevelingData.Servers[824824193001979924].Users[Id].TotalXP / 5);
            }
            else
            {
                UserBalances.Add(Id, 0);
            }
        }
        public static void AddBalance(ulong user, double amount)
        {
            LoadData();
            UserBalances[user] += amount;
            WriteData();
        }
        public static void RemoveBalance(ulong user, double amount)
        {
            LoadData();
            if (UserBalances[user] >= amount) UserBalances[user] -= amount;
            else throw new ArgumentException();

            WriteData();
        }
        public static void TransferBalance(ulong user1, ulong user2, double amount)
        {
            LoadData();
            if (UserBalances[user1] >= amount)
            {
                UserBalances[user1] -= amount;
                UserBalances[user2] += amount;
            }
            else throw new ArgumentException();

            WriteData();
        }

        public static void LoadData()
        {
            UserBalances = new Dictionary<ulong, double>();
            KONNode node = KONParser.Default.Parse(SensitiveInformation.DecryptDataFile(File.ReadAllText($@"{Utils.localDataPath}/Dripcoin.cb")));
            foreach (KONNode userNode in node.Children)
            {
                UserBalances.Add((ulong)userNode.Values["id"], (double)userNode.Values["balance"]);
            }
        }

        public static void WriteData()
        {
            if (Program.doNotWrite) return;
            KONNode node = new KONNode("DRIPCOIN_DATA");
            foreach (KeyValuePair<ulong, double> user in UserBalances)
            {
                KONNode userNode = new KONNode("USER");
                userNode.AddValue("id", user.Key);
                userNode.AddValue("balance", user.Value);
                node.AddChild(userNode);
            }
            File.WriteAllText($@"{Utils.localDataPath}/Dripcoin.cb", SensitiveInformation.EncryptDataFile(KONWriter.Default.Write(node)));
        }
    }
    [Group("dripcoin"), Hidden, RequireGuild]
    public class DripcoinCommands : BaseCommandModule
    {
        [Command("balance")]
        public async Task Balance(CommandContext ctx, [RemainingText] DiscordMember? member = null)
        {
            if (ctx.Guild.Id != 824824193001979924) return;
            member ??= ctx.Member;
            if (!Dripcoin.UserBalances.ContainsKey(member!.Id)) Dripcoin.CreateUser(member.Id);
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
            eb.WithTitle($"Dripcoin Wallet Balance: {member.Username}");
            eb.WithDescription($"{Dripcoin.UserBalances[member.Id]} Dripcoin");
            await ctx.RespondAsync(embed: eb.Build());
        }

        [Command("transfer"), Hidden, RequireGuild]
        public async Task Transfer(CommandContext ctx, double amount, [RemainingText] DiscordMember member)
        {
            try
            {
                Dripcoin.TransferBalance(ctx.User.Id, member.Id, amount);
                await ctx.RespondAsync($"Successfully transferred {amount} Dripcoin to <@!{member.Id}>");
            }
            catch (ArgumentException)
            {
                await ctx.RespondAsync("You don't have that much balance!");
            }
        }
    }
}