using System.Text;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Hashes;

public static class HashHGet
{
    /// <summary>
    /// HGET key field
    /// </summary>
    [Command(Key = "HGET")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var hashKey = package.Parameters[0].Trim();
            _cache.TryGet<ICacheEntry>(hashKey, out var entry);
            if (entry is not HashCacheEntry hashCacheEntry)
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            var fieldKey = package.Parameters[1];
            var value = hashCacheEntry.Get(fieldKey);

            hashCacheEntry.LastAccessedAt = DateTimeOffset.Now;
            var response = value is not null
                ? Encoding.UTF8.GetString(value)
                : Nil.ToString();
            await session.SendStringAsync($"{response}\n");
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

            if (parameters.Any(parameter => parameter.Length * 2 > StringKeySizeLimitInBytes))
            {
                return ValueTask.FromResult(ValidationResult.Failure("String key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}