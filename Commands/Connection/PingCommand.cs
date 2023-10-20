using PyroCache.Commands.Common;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Connection;

public static class Ping
{
    /// <summary>
    /// PING [message]
    /// </summary>
    [Command(Key = "PING")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            string? message = package.Parameters.Length == 2
                ? package.Parameters[1].Trim()
                : null;
            if (message is not null)
            {
                await session.SendStringAsync($"{message}\n");
            }
            else
            {
                await session.SendStringAsync("PONG\n");
            }
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}