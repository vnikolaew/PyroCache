using System.Globalization;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;
using Enumerable = System.Linq.Enumerable;

namespace PyroCache.Commands.SortedSets;

public static class SortedSetZMPop
{
    /// <summary>
    /// ZMPOP numkeys key [key ...] <MIN | MAX> [COUNT count]
    /// </summary>ZPOPMAX
    [Command(Key = "ZMPOP")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var numKeys = int.Parse(package.Parameters[0]);

            int countIndex = package.Parameters.IndexOf(p => p is "COUNT");
            int minMaxIndex = package.Parameters.IndexOf(p => p is "MIN" or "MAX");

            var endIndex = minMaxIndex != -1 ? minMaxIndex : countIndex != 1 ? countIndex : 0;

            var keys = package.Parameters[1..endIndex].ToArray();
            var min = minMaxIndex != -1 && package.Parameters[minMaxIndex] is "MIN";
            var count = countIndex != -1 ? int.Parse(package.Parameters[^1]) : 1;

            var results =
                new Dictionary<string, List<SortedSetEntry>>();
            var sortedSets = keys.Select(key =>
                    _cache.TryGet<SortedSetCacheEntry>(key, out var set) ? set : null!)
                .Where(_ => _ is not null)
                .ToList();

            foreach (var sortedSet in sortedSets)
            {
                results[sortedSet!.Key] ??= new();
                var popCount = Math.Min(count, sortedSet.Size);

                SortedSetEntry entry;
                if (min)
                {
                    foreach (var _ in Enumerable.Range(0, popCount))
                    {
                        sortedSet.PopMin(out entry!);
                        results[sortedSet.Key].Add(entry);
                    }
                }
                else
                {
                    foreach (var _ in Enumerable.Range(0, popCount))
                    {
                        sortedSet.PopMax(out entry!);
                        results[sortedSet.Key].Add(entry);
                    }
                }
            }

            var response = FormResponse(results);
            await session.SendStringAsync($"{response}\n");
        }

        private static string FormResponse(
            Dictionary<string, List<SortedSetEntry>> results)
            => results.Select((kv,
                index) =>
            {
                var response = $"{index * 2 + 1}) {kv.Key}";
                response += $"{index * 2 + 2})";
                response +=
                    kv.Value.Select((entry,
                        i) =>
                    {
                        var res = $"{i + 1})";
                        res += $" 1) {entry.Value}\n";
                        res += $"\t 2) {entry.Score:F}";

                        return res;
                    }).Join("\n");

                return response;
            }).Join("\n");
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

            var indexOfMinMax = Array.FindIndex(
                parameters,
                p => p is "MIN" or "MAX");
            if (indexOfMinMax != -1)
            {
                var keys = parameters[1..indexOfMinMax].ToArray();
                if (keys.Any(key => key.Length * 2 > StringKeySizeLimitInBytes))
                {
                    return ValueTask.FromResult(ValidationResult.Failure("Hash key exceeds maximum limit of 1KB."));
                }
            }

            if (parameters[^2] is "COUNT")
            {
                var count = parameters[^1].Trim();
                if (!int.TryParse(count, NumberStyles.Integer, new NumberFormatInfo(), out _))
                {
                    return ValueTask.FromResult(ValidationResult.Failure("Number of keys should be an integer."));
                }
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}