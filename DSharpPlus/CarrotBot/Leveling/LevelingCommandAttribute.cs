using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;

namespace CarrotBot.Leveling
{
    /// <summary>
    /// Marks a command as part of the bot's leveling module.
    /// </summary>
    public sealed class LevelingCommandAttribute : Attribute { }
}