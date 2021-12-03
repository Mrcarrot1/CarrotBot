using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace CarrotBot.Leveling
{
    /// <summary>
    /// Used to require that leveling be enabled(default) or disabled to run a command. Always fails for a DM channel.
    /// </summary>
    public class RequireLevelingAttribute : CheckBaseAttribute
    {
        /// <summary>
        /// Whether leveling needs to be enabled to run this command. If true, leveling must be enabled. If false, leveling must be disabled.
        /// </summary>
        /// <value></value>
        public bool Enabled { get; private set; }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            if(ctx.Channel.IsPrivate) return Task.FromResult(false);

            if(LevelingData.Servers.ContainsKey(ctx.Guild.Id)) return Task.FromResult(Enabled);

            return Task.FromResult(!Enabled);
        }

        public RequireLevelingAttribute()
        {
            Enabled = true;
        }
        public RequireLevelingAttribute(bool enabled)
        {
            Enabled = enabled;
        }
    }
}