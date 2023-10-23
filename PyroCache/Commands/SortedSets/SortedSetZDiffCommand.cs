using System.Globalization;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.SortedSets;

public static class SortedSetZDiff
{
    /// <summary>
    /// ZDIFF numkeys key [key ...] [WITHSCORES]
    /// </summary>
    [Command(Key = "ZDIFF")]
    public sealed class Command : BasePyroCommand
    {
        private bool _withScores;

        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var setKey = package.Parameters[1].Trim();
            _cache.TryGet<ICacheEntry>(setKey, out var entry);
            if (entry is not SortedSetCacheEntry sortedSetCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            var numKeys = int.Parse(package.Parameters[0].Trim());
            if (package.Parameters[^1] == "WITHSCORES") _withScores = true;

            var otherSetKeys = package.Parameters[2..(_withScores ? -1 : 0)].ToArray();
            var otherSets = otherSetKeys
                .Select(key => _cache.TryGet<SortedSetCacheEntry>(key, out var setCacheEntry) ? setCacheEntry : default)
                .Where(_ => _ is not null)
                .ToList();

            sortedSetCacheEntry.LastAccessedAt = DateTimeOffset.Now;
            string response;
            int currIdx;
            if (otherSets.Count == 0)
            {
                currIdx = 1;
                response = sortedSetCacheEntry.Value
                    .Take(numKeys)
                    .Select(e =>
                    {
                        var res = $"{currIdx}) {e.Value}\n";
                        if (_withScores) res += $"{currIdx + 1}) {e.Score:F}\n";
                        currIdx += _withScores ? 2 : 1;
                        return res;
                    })
                    .Join("\n");

                await session.SendStringAsync($"{response}\n");
                return;
            }

            var diffedSet = sortedSetCacheEntry.DiffWith(otherSets);

            currIdx = 1;
            response = diffedSet
                .Take(numKeys)
                .Select(e =>
                {
                    var res = $"{currIdx}) {e.Value}\n";
                    if (_withScores) res += $"{currIdx + 1}) {e.Score:F}\n";
                    currIdx += _withScores ? 2 : 1;
                    return res;
                })
                .Join("\n");

            await session.SendStringAsync($"{response}\n");
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

            var numKeys = parameters[0].Trim();
            if (!int.TryParse(numKeys, NumberStyles.Integer, new NumberFormatInfo(), out _))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Number of keys should be an integer."));
            }

            var setKey = parameters[1].Trim();
            if (setKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Hash key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}