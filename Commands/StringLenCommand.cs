using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands;

public static class StringLen
{
    /// <summary>
    /// STRLEN ke 
    /// </summary>
    [Command(Key = "STRLEN")]
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
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            if (cacheEntry!.IsExpired)
            {
                // Set item for purging:
                SetItemForPurging(session, cacheEntry);
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            cacheEntry.LastAccessedAt = DateTimeOffset.Now;
            await session.SendStringAsync($"{cacheEntry.Value.Length}\n");
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length != 1)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            var stringKey = parameters[0].Trim();
            if (stringKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("String key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}