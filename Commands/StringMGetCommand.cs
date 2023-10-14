using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands;

public static class StringMGet
{
    /// <summary>
    /// MGET key_1 key_2 [...]
    /// </summary>
    [Command(Key = "MGET")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var stringKeys = package.Parameters.ToArray();
            var stringValues = new List<string?>();

            foreach (var stringKey in stringKeys)
            {
                if (!_cache.TryGet<StringCacheEntry>(stringKey, out var cacheEntry))
                {
                    stringValues.Add(null);
                    continue;
                }

                if (cacheEntry!.IsExpired)
                {
                    // Set item for purging:
                    SetItemForPurging(session, cacheEntry);
                    stringValues.Add(null);
                    continue;
                }

                cacheEntry.LastAccessedAt = DateTimeOffset.Now;
                stringValues.Add(cacheEntry.Value);
            }

            foreach (var stringValue in stringValues)
            {
                if(stringValue is null)
                    await session.SendStringAsync($"{Nil}\n");
                else 
                    await session.SendStringAsync($"{stringValue}\n");
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
            if (parameters.Length == 0)
            {
                return ValueTask.FromResult(ValidationResult.Failure("String keys should be at least 1."));
            }
            
            if (parameters.Any(p => p.Length * 2 > StringKeySizeLimitInBytes))
            {
                return ValueTask.FromResult(ValidationResult.Failure("String key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}