using System.Collections.Immutable;
using System.Globalization;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.SortedSets;

public static class SortedSetZDiffStore
{
    /// <summary>
    /// ZDIFFSTORE destination numkeys key [key ...]
    /// </summary>
    [Command(Key = "ZDIFFSTORE")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var numKeys = int.Parse(package.Parameters[1].Trim());

            var setKey = package.Parameters[0].Trim();
            _cache.TryGet<ICacheEntry>(setKey, out var destinationEntry);
            if (destinationEntry is not null && destinationEntry is not SortedSetCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            var destinationSetCacheEntry = (destinationEntry as SortedSetCacheEntry)!;

            var sourceKey = package.Parameters[2];
            _cache.TryGet<ICacheEntry>(sourceKey, out var sourceEntry);
            if (sourceEntry is not null && sourceEntry is not SortedSetCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            var sourceSetCacheEntry = (sourceEntry as SortedSetCacheEntry)!;

            var otherSetKeys = package.Parameters[3..].ToArray();
            var otherSets = otherSetKeys
                .Select(key => _cache.TryGet<SortedSetCacheEntry>(key, out var setCacheEntry) ? setCacheEntry : default)
                .Where(_ => _ is not null)
                .ToList();

            sourceSetCacheEntry.LastAccessedAt = DateTimeOffset.Now;
            destinationSetCacheEntry.LastAccessedAt = DateTimeOffset.Now;

            var resultSet = sourceSetCacheEntry.DiffWith(otherSets);
            destinationSetCacheEntry.Value = new SortedSet<SortedSetEntry>(resultSet.Take(numKeys));

            await session.SendStringAsync($"{destinationSetCacheEntry.Size}\n");
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length < 3)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            var destinationKey = parameters[0].Trim();
            if (destinationKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Hash key exceeds maximum limit of 1KB."));
            }

            var numKeys = parameters[1].Trim();
            if (!int.TryParse(numKeys, NumberStyles.Integer, new NumberFormatInfo(), out _))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Number of keys should be an integer."));
            }


            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}