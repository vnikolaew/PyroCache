using System.Globalization;
using PyroCache.Commands.Common;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Connection;

public static class ClientPause
{
    /// <summary>
    /// CLIENT PAUSE
    /// </summary>
    [Command(Key = "CLIENT PAUSE")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var timeout = int.Parse(package.Parameters[2].Trim());
            await (session as PyroSession)!.Pause(timeout);
            
            await session.SendStringAsync($"{Ok}\n");
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length != 1)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            string timeout = parameters[1].Trim();
            if (!int.TryParse(timeout, NumberStyles.Integer, new NumberFormatInfo(), out _))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Timeout must be an integer."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}