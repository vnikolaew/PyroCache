using System.Globalization;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.SortedSets;

public static class SortedSetZUnion
{
    /// <summary>
    /// ZUNION numkeys key [key ...] [WEIGHTS weight [weight ...]]
    /// [AGGREGATE <SUM | MIN | MAX>] [WITHSCORES]
    /// </summary>
    [Command(Key = "ZUNION")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var numKeys = int.Parse(package.Parameters[0].Trim());
            var keys = package.Parameters[1..(1 + numKeys)].ToArray();
            var withScores = package.Parameters.Any(p => p is "WITHSCORES");

            var sortedSets = keys
                .Select(key => _cache.TryGet<SortedSetCacheEntry>(key, out var entry) ? entry : null)
                .Where(_ => _ is not null)
                .Cast<SortedSetCacheEntry>()
                .ToList();

            if (sortedSets.Count == 0)
            {
                await session.SendStringAsync($"{Nil}\n");
            }

            var combinedSet = new SortedSet<SortedSetEntry>(
                sortedSets.SelectMany(s => s.Value)
            );

            string response;
            if (withScores)
            {
                response = combinedSet
                    .Select((e,
                            index) => $"{index * 2 + 1}) {e.Value}\n{index * 2 + 2}) {e.Score:F}")
                    .Join("\n");
            }
            else
            {
                response = combinedSet
                    .Select((e,
                            index) => $"{index + 1}) {e.Value}")
                    .Join("\n");
            }

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
                return ValueTask.FromResult(ValidationResult.Failure("Number of keys must be an integer."));
            }

            var keys = parameters[1..].ToArray();
            if (keys.Any(k => k.Length * 2 > StringKeySizeLimitInBytes))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Set member exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}