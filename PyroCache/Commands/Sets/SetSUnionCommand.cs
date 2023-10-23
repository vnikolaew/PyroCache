using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Sets;

public static class SetSUnion
{
    /// <summary>
    /// SUNION key [key ...]
    /// </summary>
    [Command(Key = "SUNION")]
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
            _cache.TryGet<ICacheEntry>(sourceKey, out var sourceCacheEntry);

            if (sourceCacheEntry is not SetCacheEntry sourceSetCacheEntry)
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            var otherSetKeys = package.Parameters[1..].ToArray();
            var otherSets = otherSetKeys
                .Select(key => _cache.TryGet<SetCacheEntry>(key, out var entry) ? entry : default)
                .Where(_ => _ is not null)
                .ToList();

            string response;
            if (otherSets.Count == 0)
            {
                response = string.Join("\n",
                    sourceSetCacheEntry.Value.Select((e,
                            i) => $"{i + 1}) {e}"));

                await session.SendStringAsync($"{response}\n");
                return;
            }

            var unionSet = sourceSetCacheEntry.UnionWith(otherSets);
            sourceSetCacheEntry.LastAccessedAt = DateTimeOffset.Now;
            response = string.Join("\n",
                unionSet.Select((e,
                        i) => $"{i + 1}) {e}"));

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

            if (parameters.Any(parameter => parameter.Length * 2 > StringKeySizeLimitInBytes))
            {
                return ValueTask.FromResult(ValidationResult.Failure("String key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}