using System.Globalization;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.SortedSets;

public static class SortedSetZInterCard
{
    /// <summary>
    /// ZINTERCARD numkeys key [key ...] [LIMIT limit]
    /// </summary>
    [Command(Key = "ZINTERCARD")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var setKey = package.Parameters[1].Trim();
            var numKeys = int.Parse(package.Parameters[0].Trim());

            _cache.TryGet<ICacheEntry>(setKey, out var entry);
            if (entry is not SortedSetCacheEntry sortedSetCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }
            sortedSetCacheEntry.LastAccessedAt = DateTimeOffset.Now;

            var otherSetKeys = package.Parameters[2..].ToArray();
            var otherSets = otherSetKeys
                .Select(key => _cache.TryGet<SortedSetCacheEntry>(key, out var setCacheEntry) ? setCacheEntry : default)
                .Where(_ => _ is not null)
                .ToList();

            otherSets.ForEach(set => set.LastAccessedAt = DateTimeOffset.Now);
            
            var resultSet = sortedSetCacheEntry.IntersectWith(otherSets);
            await session.SendStringAsync($"{resultSet.Count}\n");
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

            var key = parameters[1].Trim();
            if (key.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Hash key exceeds maximum limit of 1KB."));
            }

            var numKeys = parameters[0].Trim();
            if (!int.TryParse(numKeys, NumberStyles.Integer, new NumberFormatInfo(), out _))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Number of keys should be an integer."));
            }

            var otherKeys = parameters[2..].ToArray();
            if (otherKeys.Any(key => key.Length * 2 > StringKeySizeLimitInBytes))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Hash key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
    
}