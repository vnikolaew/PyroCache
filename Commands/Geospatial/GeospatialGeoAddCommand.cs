using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Geospatial;

public static class GeospatialGeoAdd
{
    /// <summary>
    /// GEOADD key [NX | XX] [CH] longitude latitude member [longitude latitude member ...]
    /// </summary>
    [Command(Key = "GEOADD")]
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
            _cache.TryGet<ICacheEntry>(indexKey, out var entry);
            if (entry is not GeospatialIndexCacheEntry geospatialIndexCacheEntry)
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }


            var newMembers = (package.Parameters.Length - 1) / 3;
            for (int i = 0; i < newMembers; i++)
            {
                var longitude = float.Parse(package.Parameters[i * 3 + 1]);
                var latitude = float.Parse(package.Parameters[i * 3 + 2]);
                var member = package.Parameters[i * 3 + 3];

                geospatialIndexCacheEntry.LastAccessedAt = DateTimeOffset.Now;
                geospatialIndexCacheEntry.Add(member, longitude, latitude);
            }

            await session.SendStringAsync($"{newMembers}\n");
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length < 4)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            var indexKey = parameters[0].Trim();
            if (indexKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Index key exceeds maximum limit of 1KB."));
            }


            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}