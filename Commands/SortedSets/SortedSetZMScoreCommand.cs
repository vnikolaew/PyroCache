using System.Globalization;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.SortedSets;

public static class SortedSetZMScore
{
    /// <summary>
    /// ZMSCORE key member [member ...]
    /// </summary>
    [Command(Key = "ZMSCORE")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var setKey = package.Parameters[0].Trim();
            var memberKeys = package.Parameters[1..].ToHashSet();

            _cache.TryGet<ICacheEntry>(setKey, out var entry);
            if (entry is not SortedSetCacheEntry sortedSetCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            sortedSetCacheEntry.LastAccessedAt = DateTimeOffset.Now;
            var members = memberKeys
                .Select(key =>
                    sortedSetCacheEntry.Value.TryGetValue(
                        new SortedSetEntry { Value = key },
                        out var member)
                        ? member.Score as float?
                        : null)
                .ToList();

            var response = members
                .Select((m,
                        i) => $"{i + 1}) {(m is null ? Nil : m)}")
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

            var key = parameters[0].Trim();
            if (key.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Hash key exceeds maximum limit of 1KB."));
            }

            var members = parameters[1..].ToArray();
            if (members.Any(key => key.Length * 2 > StringKeySizeLimitInBytes))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Hash key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}