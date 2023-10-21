using System.Text;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Generic;

public static class Rename
{
    /// <summary>
    /// RENAME key newkey
    /// </summary>
    [Command(Key = "RENAME")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var existingKey = package.Parameters[0].Trim();
            var newKey = package.Parameters[1].Trim();
            

            if (!_cache.TryGet<ICacheEntry>(existingKey, out var entry))
            {
                await session.SendStringAsync($"{Nil}\n");
            }
            
            if (_cache.TryGet<ICacheEntry>(newKey, out _))
            {
                _cache.TryRemove(newKey, out _);
            }

            entry!.Touch();
            entry.Key = newKey;
            await session.SendStringAsync($"{Ok}\n");
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
            
            if (parameters[0].Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Cache key exceeds maximum limit of 1KB."));
            }
            
            if (parameters[1].Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Cache key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}