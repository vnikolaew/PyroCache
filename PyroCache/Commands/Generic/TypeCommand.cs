using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Generic;

public static class Type
{
    /// <summary>
    /// TYPE key
    /// </summary>
    [Command(Key = "TYPE")]
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
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            var type = entry switch
            {
                StringCacheEntry _ => "string",
                ListCacheEntry _ => "list",
                SetCacheEntry _ => "set",
                SortedSetCacheEntry _ => "zset",
                HashCacheEntry _ => "hash",
                _ => Nil.ToString()
            };

            await session.SendStringAsync($"{type}\n");
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