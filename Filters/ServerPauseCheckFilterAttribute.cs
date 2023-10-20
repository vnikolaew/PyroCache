using PyroCache.Extensions;
using SuperSocket.Command;

namespace PyroCache.Filters;

public sealed class ServerPauseCheckFilterAttribute : AsyncCommandFilterAttribute
{
    public override async ValueTask<bool> OnCommandExecutingAsync(
        CommandExecutingContext commandContext)
    {
        if (commandContext.Session is PyroSession && PyroSession.IsPaused)
        {
            commandContext.Session.CancelCommand();
            await commandContext.Session.SendStringAsync("PAUSED.\n");
        }
        
        return true;
    }

    public override ValueTask OnCommandExecutedAsync(CommandExecutingContext commandContext) => ValueTask.CompletedTask;
}