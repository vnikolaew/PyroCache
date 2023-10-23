using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.SortedSets;

public class SortedSetZRem
{
    /// <summary>
    /// ZREM key member [member ...]
    /// [WITHSCORES]
    /// </summary>
    [Command(Key = "ZREM")]
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
            var setMembers = package.Parameters[1..].ToHashSet();

            _cache.TryGet<ICacheEntry>(setKey, out var setEntry);
            if (setEntry is not SortedSetCacheEntry sortedSetCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            var removedCount = sortedSetCacheEntry
                .Value
                .RemoveWhere(e => setMembers.Contains(e.Value));
            await session.SendStringAsync($"{removedCount}\n");
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
            if (members.Any(m => m.Length * 2 > StringKeySizeLimitInBytes))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Hash key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}