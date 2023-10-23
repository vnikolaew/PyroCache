using System.Globalization;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Generic;

public static class PExpire
{
    /// <summary>
    /// PEXPIRE key milliseconds
    /// </summary>
    [Command(Key = "PEXPIRE")]
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
            var millis = long.Parse(package.Parameters[1].Trim());

            if (!_cache.TryGet<ICacheEntry>(key, out var entry))
            {
                await session.SendStringAsync($"{Zero}\n");
            }

            entry!.LastAccessedAt = DateTimeOffset.Now;
            entry.TimeToLive = TimeSpan.FromMilliseconds(millis);

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
            if (parameters.Length < 2)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            if (parameters[0].Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Cache key exceeds maximum limit of 1KB."));
            }

            string millis = parameters[1].Trim();
            if (!long.TryParse(millis, NumberStyles.Integer, new NumberFormatInfo(), out _))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Seconds must be an integer."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}