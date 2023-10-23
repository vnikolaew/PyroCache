using System.IO.Pipes;
using PyroCache.Commands.Common;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Connection;

public static class ClientSetName
{
    /// <summary>
    /// CLIENT SETNAME connection-name
    /// </summary>
    [Command(Key = "CLIENT SETNAME")]
    public sealed class Command : BasePyroCommand
    {
        public static readonly object ClientNameKey = new();

        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var connectionName = package.Parameters[2].Trim();
            (session as PyroSession)!.ClientName = connectionName;

            await session.SendStringAsync($"{Ok}\n");
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length != 2)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}