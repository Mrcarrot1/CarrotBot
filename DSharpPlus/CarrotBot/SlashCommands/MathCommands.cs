using System;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using NCalcAsync;

namespace CarrotBot.SlashCommands
{
    [SlashCommandGroup("math", "Math commands")]
    public class MathCommands : ApplicationCommandModule
    {
        [SlashCommand("add", "Adds two numbers")]
        public async Task Add(InteractionContext ctx, [Option("number1", "The first number.")] double num1, [Option("number2", "The second number.")] double num2)
        {
            await ctx.RespondAsync($"{num1 + num2}");
        }
        [SlashCommand("subtract", "Subtracts the second number from the first")]
        public async Task Subtract(InteractionContext ctx, [Option("number1", "The first number.")] double num1, [Option("number2", "The second number.")] double num2)
        {
            await ctx.RespondAsync($"{num1 - num2}");
        }
        [SlashCommand("multiply", "Multiplies two numbers")]
        public async Task Multiply(InteractionContext ctx, [Option("number1", "The first number.")] double num1, [Option("number2", "The second number.")] double num2)
        {
            await ctx.RespondAsync($"{num1 * num2}");
        }
        [SlashCommand("divide", "Divides the first number by the second")]
        public async Task Divide(InteractionContext ctx, [Option("number1", "The first number.")] double num1, [Option("number2", "The second number.")] double num2)
        {
            await ctx.RespondAsync($"{num1 / num2}");
        }
        [SlashCommand("sqrt", "Finds the square root of a number.")]
        public async Task Sqrt(InteractionContext ctx, [Option("number", "The number to find the square root of.")] double num)
        {
            await ctx.RespondAsync($"{Math.Sqrt(num)}");
        }
        //Slash commands claim another victim: command overloads
        /*[SlashCommand("round", "Rounds a number to the nearest integer.")]
        public async Task Round(InteractionContext ctx, [Option("number", "The number to round.")]double value)
        {
            await ctx.RespondAsync($"{Math.Round(value)}");
        }*/
        [SlashCommand("round", "Rounds a number to the specified number of digits.")]
        public async Task Round(InteractionContext ctx, [Option("number", "The number to round.")] double value, [Option("digits", "The number of digits to round to.")] long? digits = null)
        {
            if (digits != null)
            {
                if (digits is > int.MaxValue or < 1)
                {
                    await ctx.RespondAsync("Invalid number of digits!");
                }
                await ctx.RespondAsync($"{Math.Round(value, (int)digits)}");
            }
            else
                await ctx.RespondAsync($"{Math.Round(value)}");
        }
        [SlashCommand("pow", "Returns the first number raised to the power of the second.")]
        public async Task Pow(InteractionContext ctx, [Option("number1", "The first number.")] double num1, [Option("number2", "The second number.")] double num2)
        {
            await ctx.RespondAsync($"{Math.Pow(num1, num2)}");
        }

        [SlashCommand("eval", "Evaluates a mathematical expression.")]
        public async Task Evan(InteractionContext ctx, [Option("expression", "The expression to evaluate.")] string expression)
        {
            await ctx.IndicateResponseAsync();
            Expression exp = new(expression);
            //if (exp.HasErrors()) await ctx.UpdateResponseAsync("Invalid expression.");
            try
            {
                await ctx.UpdateResponseAsync($"{expression} = {await exp.EvaluateAsync() ?? "Invalid expression."}");
            }
            catch (Exception e)
            {
                await ctx.UpdateResponseAsync(e.Message);
            }
        }
    }
}