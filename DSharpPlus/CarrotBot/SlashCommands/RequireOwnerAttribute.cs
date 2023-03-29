using System.Threading.Tasks;
using DSharpPlus.SlashCommands;

namespace CarrotBot.SlashCommands;

public class RequireOwnerAttribute : SlashCheckBaseAttribute
{
    public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
    {
        bool ok = ctx.User.Id == Program.Mrcarrot!.Id;
        if (!ok)
        {
            await ctx.RespondAsync("You don't have permission to do that!", true);
        }
        return await Task.Run(() => ok);

    }
}