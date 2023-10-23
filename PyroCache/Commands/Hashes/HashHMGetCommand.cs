using System.Text;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Hashes;

public static class HashHMGet
{
    /// <summary>
    /// HMGET key
    /// </summary>
    [Command(Key = "HMGET")]
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
            var hashFieldKeys = package.Parameters[1..].ToArray();
            _cache.TryGet<ICacheEntry>(hashKey, out var entry);

            string response;
            if (entry is not HashCacheEntry hashCacheEntry)
            {
                response = hashFieldKeys
                    .Select((f,
                            i) => $"{i + 1}) {f}")
                    .Join("\n");

                await session.SendStringAsync($"{response}\n");
                return;
            }

            hashCacheEntry.LastAccessedAt = DateTimeOffset.Now;
            var fields = hashCacheEntry.MultiGet(hashFieldKeys);
            response = fields
                .Select((f, i) =>
                {
                    var value = f is null
                        ? Nil
                        : new ReadOnlyMemory<char>(Encoding.UTF8.GetChars(f));
                    return $"{i + 1}) {value}";
                })
                .Join("\n");

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
            if (parameters.Length < 2)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            if (parameters.Any(parameter => parameter.Trim().Length * 2 > StringKeySizeLimitInBytes))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Hash key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}