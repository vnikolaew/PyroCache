using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Sets;

public static class SetSIsMember
{
    /// <summary>
    /// SISMEMBER key member
    /// </summary>
    [Command(Key = "SISMEMBER")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var setKey = package.Parameters[0].Trim();
            var setMember = package.Parameters[1].Trim();
            _cache.TryGet<ICacheEntry>(setKey, out var cacheEntry);

            if (cacheEntry is not SetCacheEntry setCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            var isMember = setCacheEntry.IsMember(setMember);
            
            setCacheEntry.LastAccessedAt = DateTimeOffset.Now;
            await session.SendStringAsync($"{(isMember ? 1 : 0)}\n");
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

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}