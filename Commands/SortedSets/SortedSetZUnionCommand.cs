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
    /// ZSCORE key member
    /// [WITHSCORES]
    /// </summary>
    [Command(Key = "ZSCORE")]
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

            _cache.TryGet<ICacheEntry>(setKey, out var setEntry);
            if (setEntry is not SortedSetCacheEntry sortedSetCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            var entry = sortedSetCacheEntry.Value
                .FirstOrDefault(e => e.Value == setMember);
            if (entry is not null)
            {
                await session.SendStringAsync($"{entry.Score:F}\n");
            }
            else
            {
                await session.SendStringAsync($"{Nil}\n");
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
            if (parameters.Length != 2)
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