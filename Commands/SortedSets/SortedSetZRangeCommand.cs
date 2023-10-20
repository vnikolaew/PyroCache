using System.Globalization;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.SortedSets;

public static class SortedSetZRangeCommand
{
    /// <summary>
    /// ZRANGE key start stop [BYSCORE | BYLEX] [REV] [LIMIT offset count]
    /// [WITHSCORES]
    /// </summary>
    [Command(Key = "ZRANGE")]
    public sealed class Command : BasePyroCommand
    {
        public enum SortBy
        {
            Lex,
            Score,
            Index
        }

        public record Pagination(int Limit, int Offset);

        private SortBy _sortBy;

        private bool _reverse;

        private Pagination? _pagination;

        private bool _withScores;

        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var setKey = package.Parameters[0].Trim();

            var start = package.Parameters[1].Trim() switch
            {
                "+inf" => int.MaxValue,
                "-inf" => int.MinValue,
                string x => int.Parse(x)
            };
            
            var stop = package.Parameters[2].Trim() switch
            {
                "+inf" => int.MaxValue,
                "-inf" => int.MinValue,
                string x => int.Parse(x)
            };

            _sortBy = package.Parameters
                .Any(p => p is "BYSCORE")
                ? SortBy.Score
                : package.Parameters.Any(p => p is "BYLEX")
                    ? SortBy.Lex
                    : SortBy.Index;

            _reverse = package.Parameters.Any(p => p is "REV");
            var limitIndex = package.Parameters.IndexOf(p => p is "LIMIT");
            _pagination = limitIndex != -1
                ? new Pagination(
                    int.Parse(package.Parameters[limitIndex + 2]),
                    int.Parse(package.Parameters[limitIndex + 1])
                )
                : null!;
            _withScores = package.Parameters.Any(p => p is "WITHSCORES");

            _cache.TryGet<ICacheEntry>(setKey, out var setEntry);
            if (setEntry is not SortedSetCacheEntry sortedSetCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }


            List<SortedSetEntry> filteredEntries = new();
            if (_sortBy == SortBy.Index)
            {
                filteredEntries = sortedSetCacheEntry.Value
                    .Skip(start)
                    .Take(stop)
                    .ToList();
            }
            else if (_sortBy == SortBy.Lex)
            {
                filteredEntries = sortedSetCacheEntry.Value
                    .OrderBy(e => e.Value)
                    .Skip(start)
                    .Take(stop)
                    .ToList();
            }
            else if (_sortBy == SortBy.Score)
            {
                filteredEntries = sortedSetCacheEntry.Value
                    .OrderBy(e => e.Score)
                    .Skip(start)
                    .Take(stop)
                    .ToList();
            }

            if (_reverse) filteredEntries.Reverse();
            if (_pagination is not null)
            {
                filteredEntries = filteredEntries
                    .Skip(_pagination.Offset)
                    .Take(_pagination.Limit)
                    .ToList();
            }

            string response;
            if (_withScores)
            {
                response = filteredEntries
                    .Select((e,
                            i) => $"{i * 2 + 1}) {e.Value}\n{i * 2 + 2}) {e.Score:F}")
                    .Join("\n");
            }
            else
            {
                response = filteredEntries
                    .Select((e,
                            i) => $"{i + 1}) {e.Value}")
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
            if (parameters.Length < 3)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            var key = parameters[0].Trim();
            if (key.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Hash key exceeds maximum limit of 1KB."));
            }

            var start = parameters[1].ToArray();
            if (!int.TryParse(start, NumberStyles.Integer, new NumberFormatInfo(), out _))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Count must be an integer."));
            }

            var stop = parameters[2].ToArray();
            if (!int.TryParse(stop, NumberStyles.Integer, new NumberFormatInfo(), out _))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Count must be an integer."));
            }

            var limitIndex = parameters.IndexOf(p => p is "LIMIT");
            if (limitIndex != -1)
            {
                var offset = parameters[limitIndex + 1];
                if (!int.TryParse(offset, NumberStyles.Integer, new NumberFormatInfo(), out _))
                {
                    return ValueTask.FromResult(ValidationResult.Failure("Count must be an integer."));
                }

                var count = parameters[limitIndex + 2];
                if (!int.TryParse(count, NumberStyles.Integer, new NumberFormatInfo(), out _))
                {
                    return ValueTask.FromResult(ValidationResult.Failure("Count must be an integer."));
                }
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}