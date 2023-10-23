using System.Reflection.Metadata.Ecma335;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.String;

public static class StringGetRange
{
    /// <summary>
    /// GETRANGE key start end
    /// </summary>
    [Command(Key = "GETRANGE")]
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
            var startIndex = int.Parse(package.Parameters[1].Trim());
            var endIndex = int.Parse(package.Parameters[2].Trim());

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

            var subString = GetSubstring(cacheEntry, startIndex, endIndex);
            cacheEntry.LastAccessedAt = DateTimeOffset.Now;

            await session.SendStringAsync($"{subString}\n");
        }

        private static string GetSubstring(
            StringCacheEntry cacheEntry,
            int startIndex,
            int endIndex)
        {
            var stringLength = cacheEntry.Value.Length;
            var normalizedStartIndex = startIndex < 0
                ? stringLength - startIndex % stringLength
                : startIndex % stringLength;
            var normalizedEndIndex = endIndex < 0
                ? stringLength - endIndex % stringLength
                : endIndex % stringLength;

            return cacheEntry.Value[normalizedStartIndex..normalizedEndIndex];
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
            if (!int.TryParse(start, out _))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Start index should be a whole number."));
            }

            var end = parameters[2].Trim();
            if (!int.TryParse(end, out _))
            {
                return ValueTask.FromResult(ValidationResult.Failure("End index should be a whole number."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}