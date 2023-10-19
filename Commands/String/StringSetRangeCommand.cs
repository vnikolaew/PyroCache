using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.String;

public static class StringSetRange
{
    /// <summary>
    /// SETRANGE key offset value
    /// </summary>
    [Command(Key = "SETRANGE")]
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
            var offset = int.Parse(package.Parameters[1].Trim());
            var value = package.Parameters[2].Trim();

            if (!_cache.TryGet<StringCacheEntry>(stringKey, out var cacheEntry))
            {
                _cache.Set(stringKey, new StringCacheEntry
                {
                    Key = stringKey,
                    Value = value
                });

                await session.SendStringAsync($"{value.Length}\n");
                return;
            }

            if (cacheEntry!.IsExpired)
            {
                // Set item for purging:
                SetItemForPurging(session, cacheEntry);
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            var charBuffer = GetNewStringValue(cacheEntry.Value, offset, value);
            cacheEntry.Value = new string(charBuffer);
            cacheEntry.LastAccessedAt = DateTimeOffset.Now;

            await session.SendStringAsync($"{cacheEntry.Value.Length}\n");
        }

        private static char[] GetNewStringValue(
            string oldValue,
            int offset,
            string newValue)
        {
            var newLength = Math.Max(offset + newValue.Length, oldValue.Length);
            var oldValueSpan = oldValue.AsSpan();

            var charBuffer = new char[newLength].AsSpan();
            oldValueSpan[..offset].CopyTo(charBuffer);
            newValue.CopyTo(charBuffer[offset..]);

            if (newLength < oldValue.Length)
            {
                oldValueSpan[newLength..].CopyTo(charBuffer[(offset + newValue.Length)..]);
            }

            return charBuffer.ToArray();
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

            var offset = parameters[1].Trim();
            if (!int.TryParse(offset, out _))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Start index should be a whole number."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}