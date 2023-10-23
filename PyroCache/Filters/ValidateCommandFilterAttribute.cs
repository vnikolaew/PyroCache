using System.Reflection;
using PyroCache.Commands.Common;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Filters;

public sealed class ValidateCommandFilterAttribute : AsyncCommandFilterAttribute
{
    public static readonly object ErrorKey = new();

    public override async ValueTask<bool> OnCommandExecutingAsync(CommandExecutingContext commandContext)
    {
        var serviceProvider = commandContext.Session.Server.ServiceProvider;
        var package = commandContext.Package as StringPackageInfo;
        var cts = new CancellationTokenSource();

        // Resolve appropriate validator:
        var validator = serviceProvider
            .GetService(
                typeof(ICommandValidator<>)
                    .MakeGenericType(commandContext.CurrentCommand.GetType()));
        if (validator is null) return true;

        // Validate command:
        var validateMethod = validator
            .GetType()
            .GetMethod("ValidateAsync", BindingFlags.Instance | BindingFlags.Public)!;
        
        var result = await (ValueTask<ValidationResult>) validateMethod.Invoke(
            validator,
            new object?[] { package!.Parameters, cts.Token })!;

        if (result.IsFailure) commandContext.Session[ErrorKey] = result.Error;
        return true;
    }

    public override ValueTask OnCommandExecutedAsync(CommandExecutingContext commandContext)
        => ValueTask.CompletedTask;

}