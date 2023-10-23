using System.Text;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Hashes;

public static class HashHStrLen
{
    /// <summary>
    /// HSTRLEN key field
    /// </summary>
    [Command(Key = "HSTRLEN")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var hashKey = package.Parameters[0].Trim();
            var hashField = package.Parameters[1].Trim();
            _cache.TryGet<ICacheEntry>(hashKey, out var entry);

            if (entry is not HashCacheEntry hashCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            var fieldValue = hashCacheEntry.Get(hashField);
            if (fieldValue is null)
            {
                await session.SendStringAsync($"{Zero}\n");
            }
            else
            {
                await session.SendStringAsync($"{fieldValue.Length / 2}\n");
            }
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length != 2)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            if (parameters.Any(p => p.Trim().Length * 2 > StringKeySizeLimitInBytes))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Hash key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}