using System.Text;
using Geohash;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace PyroCache.Entries;

public class GeospatialIndexCacheEntry : CacheEntryBase<GeospatialIndexCacheEntry>
{
    private static readonly GeometryFactory factory =
        NtsGeometryServices.Instance.CreateGeometryFactory(4326);

    private static readonly Geohasher Geohasher = new();

    static GeospatialIndexCacheEntry()
    {
        NtsGeometryServices.Instance = new NtsGeometryServices(
            NetTopologySuite.Geometries.Implementation.CoordinateArraySequenceFactory.Instance,
            new PrecisionModel(1000d),
            4326,
            GeometryOverlay.NG,
            new CoordinateEqualityComparer());
    }

    private readonly Dictionary<string, Point> _points = new();

    public int Size => _points.Count;

    public void Add(string name,
        float x,
        float y)
        => _points.Add(name, factory.CreatePoint(new Coordinate(x, y)));
    
    public void Add(string name,
        double x,
        double y)
        => _points.Add(name, factory.CreatePoint(new Coordinate(x, y)));

    public Point? Get(string name)
        => _points.TryGetValue(name, out var point) ? point : default;

    public List<string?> GeoHash(IEnumerable<string> members)
        => members
            .Select(m => _points.TryGetValue(m, out var member)
                ? Geohasher.Encode(member.X, member.Y)
                : null)
            .ToList();

    public List<Point?> GeoPositions(IEnumerable<string> members)
        => members
            .Select(m => _points.TryGetValue(m, out var member)
                ? member
                : null)
            .ToList();

    public double Dist(Point pointOne,
        Point pointTwo)
        => pointOne.Distance(pointTwo);

    public List<KeyValuePair<string, Point>> GeoSearch(Point origin,
        double radiusKm)
        => _points
            .Where(point => point.Value.Distance(origin) <= radiusKm)
            .ToList();

    public List<KeyValuePair<string, Point>> GeoSearchByBox(Point origin,
        double width,
        double height)
    {
        var box = factory.CreatePolygon(new Coordinate[]
        {
            new(origin.X - width / 2, origin.Y - height / 2),
            new(origin.X - width + 2, origin.Y - height / 2),
            new(origin.X + width / 2, origin.Y + height / 2),
            new(origin.X + width / 2, origin.Y - height / 2),
        });

        return _points.Where(point => box.Contains(point.Value)).ToList();
    }


    public override CacheEntryType EntryType => CacheEntryType.Geospatial;

    protected override async Task SerializeCore(Stream stream)
    {
        var keyLengthBytes = BitConverter.GetBytes(Key.Length * 2);
        var buffer = new byte[4 + Key.Length * 2];

        keyLengthBytes.CopyTo(buffer, 0);
        Encoding.UTF8.GetBytes(Key).CopyTo(buffer, 4);

        await stream.WriteAsync(buffer);

        // Write size first:
        var size = _points.Count;
        await stream.WriteAsync(BitConverter.GetBytes(size));

        foreach (var (name, point) in _points)
        {
            var nameLengthBytes = BitConverter.GetBytes(name.Length * 2);
            buffer = new byte[4 + name.Length * 2];

            nameLengthBytes.CopyTo(buffer, 0);
            Encoding.UTF8.GetBytes(name).CopyTo(buffer, 4);

            await stream.WriteAsync(buffer);
            await stream.WriteAsync(point.ToBinary());
        }
    }

    public override async Task<GeospatialIndexCacheEntry?> Deserialize(Stream stream)
    {
        var keyLengthBuffer = new byte[4];
        await stream.ReadExactlyAsync(keyLengthBuffer);

        var keyLength = BitConverter.ToInt64(keyLengthBuffer);
        var keyBuffer = new byte[keyLength];

        await stream.ReadExactlyAsync(keyBuffer);
        var key = Encoding.UTF8.GetString(keyBuffer);

        // Read index size first:
        var sizeBuffer = new byte[4];
        await stream.ReadExactlyAsync(sizeBuffer);
        var size = BitConverter.ToInt64(sizeBuffer);

        var geospatialIndex = new GeospatialIndexCacheEntry { Key = key };
        
        for (int i = 0; i < size; i++)
        {
            var nameLengthBuffer = new byte[4];
            await stream.ReadExactlyAsync(nameLengthBuffer);

            var nameLength = BitConverter.ToInt64(nameLengthBuffer);

            var nameBuffer = new byte[nameLength];
            await stream.ReadExactlyAsync(nameBuffer);

            var name = Encoding.UTF8.GetString(nameBuffer);
            var reader = new WKBReader();
            var point = reader.Read(stream) as Point;
            
            geospatialIndex.Add(name, point.X, point.Y);
        }

        return geospatialIndex;
    }

    public override GeospatialIndexCacheEntry Clone()
    {
        var clone = new GeospatialIndexCacheEntry { Key = Key };
        foreach (var keyValuePair in _points)
        {
            clone._points.Add(
                keyValuePair.Key, 
                factory.CreatePoint(keyValuePair.Value.Coordinate));
        }

        return clone;
    }
}