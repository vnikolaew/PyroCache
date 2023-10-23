using System.Globalization;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Hashes;

public static class HashHIncrBy
{
    /// <summary>
    /// HINCRBY key field increment
    /// </summary>
    [Command(Key = "HINCRBY")]
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
            if (entry is not HashCacheEntry hashCacheEntry)
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            hashCacheEntry.LastAccessedAt = DateTimeOffset.Now;

            var fieldKey = package.Parameters[1].Trim();
            var value = hashCacheEntry.Get(fieldKey);
            if (value is null)
            {
                hashCacheEntry.Set(fieldKey, "1"u8.ToArray());
                await session.SendStringAsync($"{One}\n");
            }
            else
            {
                var incrementBy = int.Parse(package.Parameters[2].Trim());
                var success = hashCacheEntry.IncrementBy(fieldKey, incrementBy, out var newValue);
                
                await session.SendStringAsync($"{(success ? newValue : 0)}\n");
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
            if (parameters.Length != 3)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            var hashKey = parameters[0].Trim();
            if (hashKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Hash key exceeds maximum limit of 1KB."));
            }

            var fieldKey = parameters[1].Trim();
            if (fieldKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Field key exceeds maximum limit of 1KB."));
            }

            var incrementBy = parameters[2].Trim();
            if (!int.TryParse(incrementBy, NumberStyles.Integer, new NumberFormatInfo(), out _))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Increment amount must be an integer."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}