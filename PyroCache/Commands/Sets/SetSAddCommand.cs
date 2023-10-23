using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Sets;

public static class SetSAdd
{
    /// <summary>
    /// SADD key member [member ...]
    /// </summary>
    [Command(Key = "SADD")]
    public sealed class Command : BasePyroCommand

    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var stringKey = package.Parameters[0].Trim();
            var stringValues = package.Parameters[1..].ToArray();
            _cache.TryGet<ICacheEntry>(stringKey, out var cacheEntry);

            if (cacheEntry is not null && cacheEntry is not SetCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            cacheEntry ??= new SetCacheEntry { Key = stringKey };

            var itemsAdded = (cacheEntry as SetCacheEntry)!.AddAll(stringValues);
            
            cacheEntry.LastAccessedAt = DateTimeOffset.Now;
            await session.SendStringAsync($"{itemsAdded}\n");
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length < 2)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            var stringKey = parameters[0].Trim();
            if (stringKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("String key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}