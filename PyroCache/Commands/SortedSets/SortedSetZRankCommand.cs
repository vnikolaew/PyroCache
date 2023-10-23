using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.SortedSets;

public static class SortedSetZRank
{
    /// <summary>
    /// ZRANK key member [WITHSCORE]
    /// [WITHSCORES]
    /// </summary>
    [Command(Key = "ZRANK")]
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
            var setMember = package.Parameters[1].Trim();

            _cache.TryGet<ICacheEntry>(setKey, out var setEntry);
            if (setEntry is not SortedSetCacheEntry sortedSetCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            var entry = sortedSetCacheEntry
                .Value
                .FirstOrDefault(e => e.Value == setMember);
            if (entry is null)
            {
                await session.SendStringAsync($"{Nil}\n");
            }

            var rank = sortedSetCacheEntry.Value.GetViewBetween(
                new SortedSetEntry { Score = float.MinValue },
                new SortedSetEntry { Score = entry!.Score })
                .Count;

            await session.SendStringAsync($"{rank}\n");
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

            var member = parameters[1].Trim();
            if (member.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Hash key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}