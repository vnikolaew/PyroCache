using System.Globalization;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.SortedSets;

public static class SortedSetZIncrByCommand
{
    /// <summary>
    /// ZINCRBY key increment member
    /// </summary>
    [Command(Key = "ZINCRBY")]
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
            var increment = float.Parse(package.Parameters[1].Trim());
            var memberKey = package.Parameters[2].Trim();

            _cache.TryGet<ICacheEntry>(setKey, out var entry);
            if (entry is not SortedSetCacheEntry sortedSetCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            if (sortedSetCacheEntry.Value.TryGetValue(
                    new SortedSetEntry { Value = memberKey },
                    out var member))
            {
                sortedSetCacheEntry.Value.Remove(member);

                member.Score += increment;
                sortedSetCacheEntry.Value.Add(member);
            }
            else
            {
                member = new SortedSetEntry
                {
                    Value = memberKey,
                    Score = increment
                };
                sortedSetCacheEntry.Value.Add(member);
            }

            await session.SendStringAsync($"{member.Score}\n");
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length != 3)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            var key = parameters[0].Trim();
            if (key.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Hash key exceeds maximum limit of 1KB."));
            }

            var increment = parameters[1].Trim();
            if (!float.TryParse(increment, NumberStyles.Float, new NumberFormatInfo(), out _))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Number of keys should be an integer."));
            }


            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}