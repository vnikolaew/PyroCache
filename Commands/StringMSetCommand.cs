using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands;

public static class StringMSet
{
    /// <summary>
    /// MSET key value [key value ...]
    /// </summary>
    [Command(Key = "MSET")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var newEntries = package.Parameters.Chunk(2).ToArray();
            foreach (var newEntry in newEntries)
            {
                var stringKey = newEntry[0].Trim();
                var stringValue = newEntry[1].Trim();
                var cacheEntry = new StringCacheEntry
                {
                    Key = stringKey,
                    Value = stringValue,
                    CreatedAt = DateTimeOffset.Now,
                    LastAccessedAt = DateTimeOffset.Now
                };

                _cache.Set(stringKey, cacheEntry);
            }
            
            await session.SendStringAsync("OK\n");
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        private const int StringValueSizeLimitInBytes = 1024 * 1024 * 512;

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length % 2 != 0)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Parameters count should be divisible by 2."));
            }

            for (var i = 0; i < parameters.Length; i++)
            {
                if (i % 2 == 0 && parameters[i].Length * 2 > StringKeySizeLimitInBytes)
                {
                    return ValueTask.FromResult(ValidationResult.Failure("String key exceeds maximum limit of 1KB."));
                }

                if (i % 2 == 1 && parameters[i].Length * 2 > StringValueSizeLimitInBytes)
                {
                    return ValueTask.FromResult(
                        ValidationResult.Failure("String value exceeds maximum limit of 512MB."));
                }
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}