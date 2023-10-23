using Microsoft.Extensions.Logging;
using PyroCache.Extensions;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Filters;

public class CommandLoggingFilterAttribute : AsyncCommandFilterAttribute
{
    private readonly ILogger<CommandLoggingFilterAttribute> _logger;

    public CommandLoggingFilterAttribute(ILogger<CommandLoggingFilterAttribute> logger)
    {
        _logger = logger;
    }


    public override ValueTask<bool> OnCommandExecutingAsync(CommandExecutingContext commandContext)
    {
        _logger.LogInformation("Incoming command '{Command}' with parameters '{Params}'",
            commandContext.CurrentCommand.GetType().Name,
            (commandContext.Package as StringPackageInfo).Parameters.Join(", "));
        
        return ValueTask.FromResult(true);
    }

    public override ValueTask OnCommandExecutedAsync(CommandExecutingContext commandContext)
        => ValueTask.CompletedTask;
}