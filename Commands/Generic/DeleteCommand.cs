using PyroCache.Commands.Common;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Generic;

public static class Delete
{
    /// <summary>
    /// DEL key [key ...]
    /// </summary>
    [Command(Key = "DEL")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var keys = package.Parameters[1..].ToArray();
            var removedCount = keys
                .Select(key => _cache.TryRemove(key, out _))
                .Count(_ => _);

            await session.SendStringAsync($"{removedCount}\n");
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length < 1)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            if (parameters[1..].Any(p => p.Length * 2 > StringKeySizeLimitInBytes))
            {
                
                return ValueTask.FromResult(ValidationResult.Failure("Cache key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}