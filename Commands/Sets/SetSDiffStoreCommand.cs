using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Sets;

public static class SetSDiffStore
{
    /// <summary>
    /// SDIFFSTORE destination key [key ...]
    /// </summary>
    [Command(Key = "SDIFFSTORE")]
    public sealed class Command : BasePyroCommand

    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var destinationKey = package.Parameters[0].Trim();
            _cache.TryGet<ICacheEntry>(destinationKey, out var cacheEntry);

            if (cacheEntry is not SetCacheEntry destinationSetCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }
            
            var diffSetKey = package.Parameters[1].Trim();
            _cache.TryGet<ICacheEntry>(diffSetKey, out var diffEntry);

            if (diffEntry is not SetCacheEntry diffSetCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            var otherSetKeys = package.Parameters[2..].ToArray();
            var otherSets = otherSetKeys
                .Select(key => _cache.TryGet<SetCacheEntry>(key, out var setCacheEntry) ? setCacheEntry : default)
                .Where(_ => _ is not null)
                .ToList();
            if (otherSets.Count == 0)
            {
                _cache.Set(destinationKey, new SetCacheEntry
                {
                    Key = destinationKey,
                    Value = diffSetCacheEntry.Value
                });
                
                await session.SendStringAsync($"{diffSetCacheEntry.Size}\n");
                return;
            }

            
            var diffedSet = diffSetCacheEntry.DiffWith(otherSets);
            var newEntry = new SetCacheEntry
            {
                Key = destinationKey,
                Value = diffedSet
            };
            _cache.Set(destinationKey, newEntry);

            diffSetCacheEntry.LastAccessedAt = DateTimeOffset.Now;
            await session.SendStringAsync($"{newEntry.Size}\n");
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length <= 2)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            if (parameters.Any(parameter => parameter.Length * 2 > StringKeySizeLimitInBytes))
            {
                return ValueTask.FromResult(ValidationResult.Failure("String key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
    
}