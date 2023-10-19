using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Sets;

public static class SetSInter
{
    /// <summary>
    /// SINTER key [key ...]
    /// </summary>
    [Command(Key = "SINTER")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var setKey = package.Parameters[0].Trim();
            _cache.TryGet<ICacheEntry>(setKey, out var cacheEntry);

            if (cacheEntry is not SetCacheEntry setCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            var otherSetKeys = package.Parameters[1..].ToArray();
            var otherSets = otherSetKeys
                .Select(key => _cache.TryGet<SetCacheEntry>(key, out var entry) ? entry : default)
                .Where(_ => _ is not null)
                .ToList();
            if (otherSets.Count == 0)
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            var intersectionSet = setCacheEntry.IntersectWith(otherSets);
            var response = string.Join("\n",
                intersectionSet.Select((e,
                        i) => $"{i + 1}) {e}"));

            setCacheEntry.LastAccessedAt = DateTimeOffset.Now;
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
            if (parameters.Length <= 1)
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