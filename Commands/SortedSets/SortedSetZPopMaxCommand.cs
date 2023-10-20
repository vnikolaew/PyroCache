using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.SortedSets;

public static class SortedSetZPopMax
{
    /// <summary>
    /// ZPOPMAX key [count]
    /// </summary>ZPOPMAX
    [Command(Key = "ZPOPMAX")]
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

            _cache.TryGet<ICacheEntry>(setKey, out var setEntry);
            if (setEntry is not SortedSetCacheEntry sortedSetCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            sortedSetCacheEntry.LastAccessedAt = DateTimeOffset.Now;
            var count = int.TryParse(package.Parameters[1], out var value)
                ? Math.Min(value, sortedSetCacheEntry.Size)
                : 1;

            var response = string.Empty;
            foreach (var index in Enumerable.Range(0, count))
            {
                if (sortedSetCacheEntry.PopMax(out var entry))
                {
                    response += $"{index * 2 + 1}) {entry.Value}\n";
                    response += $"{index * 2 + 2}) {entry.Score:F}";
                }
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
            if (parameters.Length < 1)
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