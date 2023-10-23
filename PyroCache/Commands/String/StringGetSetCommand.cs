using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.String;

public static class StringGetSet
{
    /// <summary>
    /// SET key seconds value
    /// </summary>
    [Command(Key = "GETSET")]
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
            var stringValue = package.Parameters[1].Trim();

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

            var oldValue = cacheEntry.Value;
            cacheEntry.Value = stringValue;
            cacheEntry.TimeToLive = default;
            cacheEntry.LastAccessedAt = DateTimeOffset.Now;
                
            await session.SendStringAsync($"{oldValue}\n");
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