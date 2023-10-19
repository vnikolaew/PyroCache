using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Sets;

public static class SetSUnionStore
{
    /// <summary>
    /// SUNIONSTORE destination key [key ...]
    /// </summary>
    [Command(Key = "SUNIONSTORE")]
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
            _cache.TryGet<ICacheEntry>(destinationKey, out var destinationCacheEntry);

            if (destinationCacheEntry is not SetCacheEntry destinationSetCacheEntry)
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            var sourceKey = package.Parameters[1].Trim();
            _cache.TryGet<ICacheEntry>(destinationKey, out var sourceCacheEntry);
            if (sourceCacheEntry is not SetCacheEntry sourceSetCacheEntry)
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            var otherSetKeys = package.Parameters[2..].ToArray();
            var otherSets = otherSetKeys
                .Select(key => _cache.TryGet<SetCacheEntry>(key, out var entry) ? entry : default)
                .Where(_ => _ is not null)
                .ToList();

            string response;
            if (otherSets.Count == 0)
            {
                _cache.Set(destinationKey, new SetCacheEntry
                {
                    Key = destinationKey,
                    Value = sourceSetCacheEntry.Value.ToHashSet(),
                });

                await session.SendStringAsync($"{sourceSetCacheEntry.Size}\n");
                return;
            }

            var unionSet = sourceSetCacheEntry.UnionWith(otherSets);
            sourceSetCacheEntry.LastAccessedAt = DateTimeOffset.Now;

            _cache.Set(destinationKey, new SetCacheEntry
            {
                Key = destinationKey,
                Value = unionSet
            });
            await session.SendStringAsync($"{unionSet.Count}\n");
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

            if (parameters.Any(parameter => parameter.Length * 2 > StringKeySizeLimitInBytes))
            {
                return ValueTask.FromResult(ValidationResult.Failure("String key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}