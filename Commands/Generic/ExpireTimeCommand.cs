using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Generic;

public static class ExpireTime
{
    /// <summary>
    /// EXPIRETIME key
    /// </summary>
    [Command(Key = "EXPIRETIME")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var key = package.Parameters[0].Trim();
            if (!_cache.TryGet<ICacheEntry>(key, out var entry))
            {
                await session.SendStringAsync($"-2\n");
            }

            entry!.LastAccessedAt = DateTimeOffset.Now;
            if (entry.TimeToLive.HasValue)
            {
                var expiryTime = entry.CreatedAt + entry.TimeToLive;
                await session.SendStringAsync($"{expiryTime.Value.ToUnixTimeSeconds()}\n");
                return;
            }

            await session.SendStringAsync("-1\n");
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length < 1)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            if (parameters[0].Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Cache key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}