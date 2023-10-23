using NetTopologySuite.Geometries;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Geospatial;

public static class GeospatialGeoSearchStoreCommand
{
    /// <summary>
    /// GEOSEARCHSTORE destination source<FROMMEMBER member |
    /// FROMLONLAT longitude latitude> <BYRADIUS radius<M | KM | FT | MI>
    /// | BYBOX width height<M | KM | FT | MI>> [ASC | DESC] [COUNT count [ANY]] [STOREDIST]
    /// </summary>
    [Command(Key = "GEOSEARCHSTORE")]
    public sealed class Command : BasePyroCommand
    {
        private string? _member;

        private Point? _origin;

        private float? _radius;

        private (double width, double height)? _boxSize;


        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var withCoords = package.Parameters.Any(p => p is "WITHCOORD");
            var withDist = package.Parameters.Any(p => p is "WITHDIST");
            var withHash = package.Parameters.Any(p => p is "WITHHASH");
            var storeDist = package.Parameters.Any(p => p is "STOREDIST");

            var indexKey = package.Parameters[1].Trim();
            var destinationKey = package.Parameters[0].Trim();

            var fromMemberIndex = package.Parameters.IndexOf(p => p is "FROMMEMBER");
            if (fromMemberIndex != -1) _member = package.Parameters[fromMemberIndex + 1];

            var fromNonLatIndex = package.Parameters.IndexOf(p => p is "FROMNONLAT");
            if (fromNonLatIndex != -1)
            {
                _origin = new Point(
                    double.Parse(package.Parameters[fromNonLatIndex + 1]),
                    double.Parse(package.Parameters[fromNonLatIndex + 2])
                );
            }

            var byRadiusIndex = package.Parameters.IndexOf(p => p is "BYRADIUS");
            if (byRadiusIndex != -1) _radius = float.Parse(package.Parameters[byRadiusIndex + 1]);

            var byBoxIndex = package.Parameters.IndexOf(p => p is "BYBOX");
            if (byBoxIndex != -1)
                _boxSize = (
                    double.Parse(package.Parameters[byBoxIndex + 1]),
                    double.Parse(package.Parameters[byBoxIndex + 2]));

            _cache.TryGet<ICacheEntry>(indexKey, out var entry);
            if (entry is not GeospatialIndexCacheEntry geospatialIndexCacheEntry)
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            geospatialIndexCacheEntry.LastAccessedAt = DateTimeOffset.Now;
            int? count = package.Parameters.IndexOf(p => p is "COUNT") is var index && index != -1
                ? int.Parse(package.Parameters[index + 1])
                : null;

            var origin = _member is not null && geospatialIndexCacheEntry.Get(_member) is { } memberPoint
                ? memberPoint
                : _origin!;

            List<KeyValuePair<string, Point>> entries = new();
            if (_radius is not null)
            {
                entries = geospatialIndexCacheEntry.GeoSearch(origin!, _radius.Value);
                if (count.HasValue) entries = entries.Take(count.Value).ToList();
            }
            else if (_boxSize is not null)
            {
                entries =
                    geospatialIndexCacheEntry.GeoSearchByBox(origin!, _boxSize.Value.width, _boxSize.Value.height);
                if (count.HasValue) entries = entries.Take(count.Value).ToList();
            }

            if (storeDist)
            {
                var newSet = new SortedSetCacheEntry
                {
                    Key = destinationKey,
                    Value = new SortedSet<SortedSetEntry>(
                        entries.Select(entry => new SortedSetEntry
                        {
                            Value = entry.Key,
                            Score = (float)geospatialIndexCacheEntry.Dist(origin, entry.Value)
                        })
                    )
                };
                
                _cache.Set(destinationKey, newSet);
                await session.SendStringAsync($"{newSet.Size}\n");
            }

            var newGeoIndex = new GeospatialIndexCacheEntry
            {
                Key = destinationKey
            };
            foreach (var (name, point) in entries)
            {
                newGeoIndex.Add(name, (float)point.X, (float)point.Y);
            }

            _cache.Set(destinationKey, newGeoIndex);
            await session.SendStringAsync($"{newGeoIndex.Size}\n");
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

            if (!parameters.Any(p => p is "FROMMEMBER" or "FROMLONLAT"))
            {
                return ValueTask.FromResult(ValidationResult.Failure(""));
            }

            if (!parameters.Any(p => p is "BYRADIUS" or "BYBOX"))
            {
                return ValueTask.FromResult(ValidationResult.Failure(""));
            }


            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}