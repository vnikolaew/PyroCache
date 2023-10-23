using PyroCache.Commands.Common;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Connection;

public static class ClientGetName
{
    /// <summary>
    /// CLIENT GETNAME
    /// </summary>
    [Command(Key = "CLIENT GETNAME")]
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
            var clientName = session[ClientNameKey];
            if (clientName is string name)
            {
                await session.SendStringAsync($"{name}\n");
            }
            else
            {
                await session.SendStringAsync($"{Nil}\n");
            }
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length != 0)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}