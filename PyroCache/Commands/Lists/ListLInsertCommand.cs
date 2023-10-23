using PyroCache.Commands.Common;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Lists;

public static class ListLInsert
{
    /// <summary>
    /// LINSERT key [key ...] timeout
    /// </summary>
    [Command(Key = "LINSERT")]
    public sealed class Command : BasePyroCommand

    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        private static readonly string[] AllowedInsertTypes = { "BEFORE", "AFTER" };

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length != 4)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            var stringKey = parameters[0].Trim();
            if (stringKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("String key exceeds maximum limit of 1KB."));
            }

            var insertType = parameters[1].Trim();
            if (!AllowedInsertTypes.Any(_ => _ == insertType))
            {
                return ValueTask.FromResult(ValidationResult.Failure(""));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}