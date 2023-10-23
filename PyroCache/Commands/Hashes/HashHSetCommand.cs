using System.Text;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Hashes;

public static class HashHSet
{
    /// <summary>
    /// HSET key field value [field value ...]
    /// </summary>
    [Command(Key = "HSET")]
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
            var hashFieldEntries = package.Parameters[1..].Chunk(2).ToArray();
            _cache.TryGet<ICacheEntry>(hashKey, out var entry);

            if (entry is not HashCacheEntry hashCacheEntry)
            {
                hashCacheEntry = new HashCacheEntry
                {
                    Key = hashKey,
                    Value = hashFieldEntries
                        .Select(e => new KeyValuePair<string, byte[]>(e[0], Encoding.UTF8.GetBytes(e[1])))
                        .ToDictionary(_ => _.Key, _ => _.Value)
                };

                _cache.Set(hashKey, hashCacheEntry);
                await session.SendStringAsync($"{hashFieldEntries.Length}\n");
                return;
            }

            var currentFieldCount = hashCacheEntry.Length;
            hashCacheEntry.LastAccessedAt = DateTimeOffset.Now;
            hashCacheEntry.MultiSet(
                hashFieldEntries
                    .Select(e =>
                        (e[0], Encoding.UTF8.GetBytes(e[1]))));

            await session.SendStringAsync($"{hashCacheEntry.Length - currentFieldCount}\n");
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length < 3)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            if (parameters.Length % 2 != -1)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            if (parameters.Where((p,
                    i) => i % 2 == 1).Any(p => p.Trim().Length * 2 > StringKeySizeLimitInBytes))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Hash key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}