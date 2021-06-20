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
    [Group("math"), Description("Math commands")]
    public class MathCommands
    {
        [Command("add"), Description("Adds two numbers")]
        public async Task Add(CommandContext ctx, double num1, double num2)
        {
            await ctx.RespondAsync($"{num1 + num2}");
        }
        [Command("subtract"), Description("Subtracts the second number from the first")]
        public async Task Subtract(CommandContext ctx, double num1, double num2)
        {
            await ctx.RespondAsync($"{num1 - num2}");
        }
        [Command("multiply"), Description("Multiplies two numbers")]
        public async Task Multiply(CommandContext ctx, double num1, double num2)
        {
            await ctx.RespondAsync($"{num1 * num2}");
        }
        [Command("divide"), Description("Divides the first number by the second")]
        public async Task Divide(CommandContext ctx, double num1, double num2)
        {
            await ctx.RespondAsync($"{num1 / num2}");
        }
        [Command("sqrt"), Description("Finds the square root of a number.")]
        public async Task Sqrt(CommandContext ctx, [Description("The number to find the square root of.")] double num)
        {
            await ctx.RespondAsync($"{Math.Sqrt(num)}");
        }
        [Command("round")]
        public async Task Round(CommandContext ctx, double value)
        {
            await ctx.RespondAsync($"{Math.Round(value)}");
        }
    }
}