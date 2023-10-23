using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Hashes;

public static class HashHLen
{
    /// <summary>
    /// HLEN key
    /// </summary>
    [Command(Key = "HLEN")]
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
            _cache.TryGet<ICacheEntry>(hashKey, out var entry);
            if (entry is not HashCacheEntry hashCacheEntry)
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            hashCacheEntry.LastAccessedAt = DateTimeOffset.Now;
            await session.SendStringAsync($"{hashCacheEntry.Length}\n");
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length != 1)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            var hashKey = parameters[0].Trim();
            if (hashKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Hash key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}