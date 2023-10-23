using System.Globalization;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.String;

public static class StringDecrBy
{
    /// <summary>
    /// DECRBY key increment
    /// </summary>
    [Command(Key = "DECRBY")]
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
            var decrementBy = package.Parameters[1].Trim();
            if (!_cache.TryGet<StringCacheEntry>(stringKey, out var cacheEntry))
            {
                cacheEntry = new StringCacheEntry
                {
                    Key = stringKey,
                    Value = "0"
                };

                _cache.Set(stringKey, cacheEntry);
                await session.SendStringAsync($"{cacheEntry.Value}\n");
                return;
            }

            if (cacheEntry!.IsExpired)
            {
                // Set item for purging:
                SetItemForPurging(session, cacheEntry);
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            if (int.TryParse(cacheEntry.Value, NumberStyles.Integer, new NumberFormatInfo(), out var value)
                && int.TryParse(decrementBy, NumberStyles.Integer, new NumberFormatInfo(), out var decrementByValue))
            {
                value -= decrementByValue;
                cacheEntry.Value = value.ToString();
                cacheEntry.LastAccessedAt = DateTimeOffset.Now;
                
                await session.SendStringAsync($"{cacheEntry.Value}\n");
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

            var stringKey = parameters[0].Trim();
            if (stringKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("String key exceeds maximum limit of 1KB."));
            }
            
            var decrementBy = parameters[1].Trim();
            if (!uint.TryParse(decrementBy, out _))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Increment should be an integer."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}