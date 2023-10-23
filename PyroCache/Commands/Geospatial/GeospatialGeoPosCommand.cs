using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Geospatial;

public static class GeospatialGeoPos
{
    /// <summary>
    /// GEOHASH key [member [member ...]]
    /// </summary>
    [Command(Key = "GEOHASH")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var indexKey = package.Parameters[0].Trim();
            var members = package.Parameters[1..].ToArray();

            _cache.TryGet<ICacheEntry>(indexKey, out var entry);
            if (entry is not GeospatialIndexCacheEntry geospatialIndexCacheEntry)
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            geospatialIndexCacheEntry.LastAccessedAt = DateTimeOffset.Now;
            var geoHashes = geospatialIndexCacheEntry.GeoHash(members);

            var response = geoHashes
                .Select((hash, index) => $"{index + 1}) {(hash is null ? Nil : hash)}")
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

            var indexKey = parameters[0].Trim();
            if (indexKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Index key exceeds maximum limit of 1KB."));
            }

            var members = parameters[1..].ToArray();
            if (members.Any(m => m.Length * 2 > StringKeySizeLimitInBytes))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Member key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}