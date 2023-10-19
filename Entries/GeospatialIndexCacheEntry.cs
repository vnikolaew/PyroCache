using Geohash;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;

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

    public void Add(string name, float x, float y)
        => _points.Add(name, factory.CreatePoint(new Coordinate(x, y)));

    public Point? Get(string name)
        => _points.TryGetValue(name, out var point) ? point : default;

    public List<string?> GeoHash(IEnumerable<string> members)
        => members
            .Select(m => _points.TryGetValue(m, out var member)
                ? Geohasher.Encode(member.X, member.Y) : null)
            .ToList();
    
    public List<Point?> GeoPositions(IEnumerable<string> members)
        => members
            .Select(m => _points.TryGetValue(m, out var member)
                ? member : null)
            .ToList();

    public double Dist(Point pointOne, Point pointTwo)
        => pointOne.Distance(pointTwo);

    public List<KeyValuePair<string, Point>> GeoSearch(Point origin, double radiusKm)
        => _points
            .Where(point => point.Value.Distance(origin) <= radiusKm)
            .ToList();

    public List<KeyValuePair<string, Point>> GeoSearchByBox(Point origin, double width, double height)
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
    
    
    public override Task Serialize(Stream stream)
    {
        throw new NotImplementedException();
    }

    public override Task<GeospatialIndexCacheEntry?> Deserialize(Stream stream)
    {
        throw new NotImplementedException();
    }
}