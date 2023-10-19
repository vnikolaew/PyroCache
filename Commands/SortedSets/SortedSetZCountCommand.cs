using System.Globalization;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.SortedSets;

public static class SortedSetZCount
{
    /// <summary>
    /// ZCOUNT key min max
    /// </summary>
    [Command(Key = "ZCOUNT")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var hashKey = package.Parameters[0].Trim();
            _cache.TryGet<ICacheEntry>(hashKey, out var entry);
            if (entry is not SortedSetCacheEntry sortedSetCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            float min = float.Parse(package.Parameters[1].Trim());
            float max = float.Parse(package.Parameters[2].Trim());

            sortedSetCacheEntry.LastAccessedAt = DateTimeOffset.Now;
            int count = sortedSetCacheEntry.GetBetween(min, max).Count;

            await session.SendStringAsync($"{count}\n");
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

            var hashKey = parameters[0].Trim();
            if (hashKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Hash key exceeds maximum limit of 1KB."));
            }

            var min = parameters[1].Trim();
            if (!float.TryParse(min, NumberStyles.Float, new NumberFormatInfo(), out _))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Min should be a float number."));
            }

            var max = parameters[2].Trim();
            if (!float.TryParse(max, NumberStyles.Float, new NumberFormatInfo(), out _))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Max should be a float number."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}