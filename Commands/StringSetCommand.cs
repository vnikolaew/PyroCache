using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands;

public static class StringSet
{
    /// <summary>
    /// SET key_1 value_1
    /// </summary>
    [Command(Key = "SET")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var stringKey = package.Parameters[0].Trim();
            var stringValue = package.Parameters[1].Trim();
            var cacheEntry = new StringCacheEntry
            {
                Key = stringKey,
                Value = stringValue,
                CreatedAt = DateTimeOffset.Now,
                LastAccessedAt = DateTimeOffset.Now,
            };
            
            _cache.Set(stringKey, cacheEntry);
            return session.SendStringAsync("OK\n");
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringValueSizeLimitInBytes = 1024 * 1024 * 512;

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

            var stringValue = parameters[1].Trim();
            if (stringValue.Length * 2 > StringValueSizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("String value exceeds maximum limit of 512MB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}