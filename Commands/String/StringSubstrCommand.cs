using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.String;

public static class StringSubstr
{
    /// <summary>
    /// SUBSTR key start end
    /// </summary>
    [Command(Key = "SUBSTR")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var stringKey = package.Parameters[0].Trim();
            if (!_cache.TryGet<StringCacheEntry>(stringKey, out var cacheEntry))
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            if (cacheEntry!.IsExpired)
            {
                // Set item for purging:
                SetItemForPurging(session, cacheEntry);
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            var itemLength = cacheEntry.Value.Length;
            var start = int.Parse(package.Parameters[1].Trim()) % itemLength;
            var end = int.Parse(package.Parameters[2].Trim()) % itemLength;

            var startIndex = start < 0 ? itemLength - start : start;
            var endIndex = end < 0 ? itemLength - end : Math.Min(end, itemLength);
            
            cacheEntry.LastAccessedAt = DateTimeOffset.Now;
            await session.SendStringAsync($"{cacheEntry.Value.AsSpan()[startIndex..endIndex]}\n");
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

            var stringKey = parameters[0].Trim();
            if (stringKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("String key exceeds maximum limit of 1KB."));
            }

            var start = parameters[1].Trim();
            if (!int.TryParse(start, out var startIndex))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Start index should be a whole number."));
            }

            var end = parameters[2].Trim();
            if (!int.TryParse(end, out var endIndex))
            {
                return ValueTask.FromResult(ValidationResult.Failure("End index should be a whole number."));
            }

            if (startIndex > endIndex)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Start index must be bigger than the end index."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}