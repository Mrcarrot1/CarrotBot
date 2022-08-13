using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System.Threading.Tasks;
namespace CarrotBot.SlashCommands;

public class RequireOwnerAttribute : SlashCheckBaseAttribute
{
    public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
    {
        return await Task.Run(() => { return ctx.User.Id == Program.Mrcarrot.Id; });
    }
}