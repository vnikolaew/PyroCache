using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Sets;

public static class SetSMove
{
    /// <summary>
    /// SMOVE source destination member
    /// </summary>
    [Command(Key = "SMOVE")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var sourceKey = package.Parameters[0].Trim();
            var destinationKey = package.Parameters[1].Trim();
            var member = package.Parameters[2].Trim();

            _cache.TryGet<ICacheEntry>(sourceKey, out var sourceCacheEntry);

            if (sourceCacheEntry is not SetCacheEntry sourceSetCacheEntry)
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }
            
            _cache.TryGet<ICacheEntry>(destinationKey, out var destinationCacheEntry);
            if (destinationCacheEntry is not SetCacheEntry destinationSetCacheEntry)
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            var isSourceMember = sourceSetCacheEntry.IsMember(member);
            sourceSetCacheEntry.LastAccessedAt = DateTimeOffset.Now;
            if (!isSourceMember)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            sourceSetCacheEntry.Remove(member);
            var isDestinationMember = destinationSetCacheEntry.IsMember(member);
            destinationSetCacheEntry.LastAccessedAt = DateTimeOffset.Now;
            if (isDestinationMember)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            destinationSetCacheEntry.Add(member);
            await session.SendStringAsync($"{One}\n");
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

            var sourceKey = parameters[0].Trim();
            if (sourceKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("String key exceeds maximum limit of 1KB."));
            }
            
            var destinationKey = parameters[1].Trim();
            if (destinationKey .Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("String key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}