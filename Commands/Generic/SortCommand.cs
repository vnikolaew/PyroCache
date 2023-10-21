using System.Text;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Generic;

public static class Sort
{
    /// <summary>
    /// SORT key [BY pattern] [LIMIT offset count] [GET pattern [GET pattern...]] [ASC | DESC] [ALPHA] [STORE destination]
    /// </summary>
    [Command(Key = "SORT")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        private Pagination? _pagination;

        private bool _ascending = true;

        private bool _sortLexicographically;

        private record Pagination(int Offset, int Count);

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var key = package.Parameters[0].Trim();
            _pagination = package.Parameters.IndexOf(p => p is "LIMIT") is int limitIndex && limitIndex != -1
                ? new Pagination(
                    int.Parse(package.Parameters[limitIndex + 1]),
                    int.Parse(package.Parameters[limitIndex + 2])
                )
                : null;
            _sortLexicographically = package.Parameters.Any(p => p is "ALPHA");
            _ascending = !package.Parameters.Any(p => p is "DESC");

            if (!_cache.TryGet<ICacheEntry>(key, out var entry))
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            if (entry is not ListCacheEntry or SetCacheEntry or SortedSetCacheEntry)
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            if (entry is ListCacheEntry listCacheEntry)
            {
                var entries = listCacheEntry.Value;
                entries = _ascending ? entries.OrderBy(e => e) : entries.OrderByDescending(e => e);
                if (_pagination is not null) entries = entries.Skip(_pagination.Offset).Take(_pagination.Count);

                var response = entries
                    .Select((e, index) => $"{index + 1}) {Encoding.UTF8.GetString(e)}")
                    .Join("\n");
                await session.SendStringAsync($"{response}\n");
            }
            else if (entry is SetCacheEntry setCacheEntry)
            {
                var entries = setCacheEntry.Value.AsEnumerable();
                entries = _ascending ? entries.OrderBy(e => e) : entries.OrderByDescending(e => e);
                if (_pagination is not null) entries = entries.Skip(_pagination.Offset).Take(_pagination.Count);

                var response = entries
                    .Select((e, index) => $"{index + 1}) {e}")
                    .Join("\n");
                await session.SendStringAsync($"{response}\n");
            }
            else if (entry is SortedSetCacheEntry sortedSetCacheEntry)
            {
                var entries = sortedSetCacheEntry.Value.AsEnumerable();
                entries = _ascending ? entries.OrderBy(e => e.Value) : entries.OrderByDescending(e => e.Value);
                if (_pagination is not null) entries = entries.Skip(_pagination.Offset).Take(_pagination.Count);

                var response = entries
                    .Select((e, index) => $"{index + 1}) {e.Value}")
                    .Join("\n");
                await session.SendStringAsync($"{response}\n");
            }
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length < 1)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            if (parameters[0].Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Cache key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}