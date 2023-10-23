using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Generic;

public class TtlCommand
{
    /// <summary>
    /// TTL key
    /// </summary>
    [Command(Key = "TTL")]
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
                await session.SendStringAsync("-2\n");
                return;
            }

            if (!entry!.TimeToLive.HasValue)
            {
                await session.SendStringAsync("-1\n");
                return;
            }

            await session.SendStringAsync($"{entry.TimeToLive.Value.Seconds}\n");
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

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}