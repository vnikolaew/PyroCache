using System.Globalization;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Generic;

public static class Copy
{
    /// <summary>
    /// COPY source destination [REPLACE]
    /// [WITHSCORES]
    /// </summary>
    [Command(Key = "COPY")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var source = package.Parameters[0].Trim();
            var destination = package.Parameters[1].Trim();

            var replace = package.Parameters.Any(p => p is "REPLACE");

            _cache.TryGet<ICacheEntry>(source, out var sourceEntry);
            if (sourceEntry is null)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            _cache.TryGet<ICacheEntry>(source, out var destinationEntry);
            if (destinationEntry is not null)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            var newEntry = sourceEntry.Clone() as ICacheEntry;
            if (replace) _cache.TryRemove(destination, out _);
            
            _cache.Set(destination, newEntry!);
            
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

            var source = parameters[0].Trim();
            if (source.Length * 2 < StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Source key exceeds maximum limit of 1KB."));
            }

            var destination = parameters[0].Trim();
            if (destination.Length * 2 < StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Destination key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}