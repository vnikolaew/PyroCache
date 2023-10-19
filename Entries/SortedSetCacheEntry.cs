using System.Text;

namespace PyroCache.Entries;

public class SortedSetCacheEntry : CacheEntryBase<SortedSetCacheEntry>
{
    public SortedSet<SortedSetEntry> Value { get; set; } = new();

    public int Size => Value.Count;

    public bool PopMax(out SortedSetEntry? max)
    {
        max = Value.Max;
        return Value.Remove(max);
    }
    
    public bool PopMin(out SortedSetEntry? max)
    {
        max = Value.Min;
        return Value.Remove(max);
    }
    
    public SortedSet<SortedSetEntry> DiffWith(IEnumerable<SortedSetCacheEntry> others)
    {
        var copy = new SortedSet<SortedSetEntry>(Value);
        foreach (var sortedSetCacheEntry in others)
        {
            copy.ExceptWith(sortedSetCacheEntry.Value);
        }

        return copy;
    }
    
    public SortedSet<SortedSetEntry> IntersectWith(IEnumerable<SortedSetCacheEntry> others)
    {
        var copy = new SortedSet<SortedSetEntry>(Value);
        foreach (var sortedSetCacheEntry in others)
        {
            copy.IntersectWith(sortedSetCacheEntry.Value);
        }

        return copy;
    }

    public SortedSet<SortedSetEntry> GetBetween(float min,
        float max)
        => Value.GetViewBetween(
            new SortedSetEntry { Score = min },
            new SortedSetEntry { Score = max });

    public bool Add(string value,
        float score)
    {
        if (Value.TryGetValue(new SortedSetEntry { Value = value },
                out var actual) && Math.Abs(actual.Score - score) > 0.01)
        {
            Value.Remove(actual);
            return Value.Add(new SortedSetEntry { Value = value, Score = score });
        }

        return Value.Add(new SortedSetEntry
        {
            Score = score,
            Value = value
        });
    }

    // SORTED_SET_LENGTH 1ST_ITEM_LENGTH 1ST_ITEM 2ND_ITEM_LENGTH 2ND_ITEM ...
    public override async Task Serialize(Stream stream)
    {
        var entriesByteArrays = Value
            .Select(e => e.GetBytes())
            .ToArray();
        
        var buffer = new byte[1 + 4 + 4 * Value.Count + entriesByteArrays.Sum(_ => _.Length)];
        buffer[0] = (byte)CacheEntryType.SortedSet;

        var currIdx = 1;
        var sizeBuffer = BitConverter.GetBytes(Value.Count);
        sizeBuffer.CopyTo(buffer, currIdx);
        currIdx += 4;

        foreach (var entryByteArray in entriesByteArrays)
        {
            var size = BitConverter.GetBytes(entryByteArray.Length);
            size.CopyTo(buffer, currIdx);
            currIdx += 4;

            entryByteArray.CopyTo(buffer, currIdx);
            currIdx += entryByteArray.Length;
        }

        await stream.WriteAsync(buffer);
    }

    public override async Task<SortedSetCacheEntry?> Deserialize(Stream stream)
    {
        throw new NotImplementedException();
    }
}

public class SortedSetEntry : IComparable<SortedSetEntry>, IEquatable<SortedSetEntry>
{
    public string Value { get; set; }

    public float Score { get; set; }

    public byte[] GetBytes()
    {
        var sizeBytes = BitConverter.GetBytes(Value.Length);
        var scoreBytes = Encoding.UTF8.GetBytes(Value);
        var valueBytes = BitConverter.GetBytes(Score);

        var buffer = new byte[8 + Value.Length * 2];

        sizeBytes.CopyTo(buffer, 0);
        valueBytes.CopyTo(buffer, 4);
        scoreBytes.CopyTo(buffer, 4 + valueBytes.Length);
        
        return buffer;
    }

    public int CompareTo(SortedSetEntry? other)
        => Score.CompareTo(other?.Score);

    public bool Equals(SortedSetEntry? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SortedSetEntry)obj);
    }

    public override int GetHashCode()
        => HashCode.Combine(Value);
}