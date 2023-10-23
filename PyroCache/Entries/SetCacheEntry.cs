using System.Text;

namespace PyroCache.Entries;

public sealed class SetCacheEntry : CacheEntryBase<SetCacheEntry>
{
    public HashSet<string> Value { get; init; } = new();

    private readonly object _setLock = new();

    public int Size => Value.Count;

    public bool Add(string item)
    {
        lock (_setLock)
        {
            return Value.Add(item);
        }
    }

    public int AddAll(string[] items)
    {
        lock (_setLock)
        {
            return items
                .Select(i => Value.Add(i))
                .Count(_ => _);
        }
    }

    public bool Remove(string item)
    {
        lock (_setLock)
        {
            return Value.Remove(item);
        }
    }

    public bool IsMember(string item)
        => Value.Contains(item);

    public HashSet<string> IntersectWith(SetCacheEntry other)
    {
        var copy = Value.ToHashSet();

        copy.IntersectWith(other.Value);
        return copy;
    }

    public HashSet<string> IntersectWith(IEnumerable<SetCacheEntry> others)
    {
        var copy = Value.ToHashSet();
        foreach (var setCacheEntry in others)
        {
            copy.IntersectWith(setCacheEntry.Value);
        }

        return copy;
    }
    
    public HashSet<string> UnionWith(IEnumerable<SetCacheEntry> others)
    {
        var copy = Value.ToHashSet();
        foreach (var setCacheEntry in others)
        {
            copy.UnionWith(setCacheEntry.Value);
        }

        return copy;
    }

    public HashSet<string> DiffWith(SetCacheEntry other)
    {
        var copy = Value.ToHashSet();

        copy.ExceptWith(other.Value);
        return copy;
    }

    public HashSet<string> DiffWith(params SetCacheEntry[] others)
        => DiffWith(others as IEnumerable<SetCacheEntry>);

    public HashSet<string> DiffWith(IEnumerable<SetCacheEntry> others)
    {
        var copy = Value.ToHashSet();

        foreach (var setCacheEntry in others)
        {
            copy.ExceptWith(setCacheEntry.Value);
        }

        return copy;
    }

    // SET_LENGTH 1ST_ITEM_LENGTH 1ST_ITEM 2ND_ITEM_LENGTH 2ND_ITEM ...
    public override CacheEntryType EntryType => CacheEntryType.Set;

    protected override async Task SerializeCore(Stream stream)
    {
        var buffer = new byte[1 + 4 + 4 * Value.Count + Value.Sum(e => e.Length * 2)];
        buffer[0] = (byte)CacheEntryType.Set;

        var currIdx = 1;
        var sizeBuffer = BitConverter.GetBytes(Value.Count);
        sizeBuffer.CopyTo(buffer, currIdx);
        currIdx += 4;

        foreach (var entry in Value)
        {
            var entrySizeBuffer = BitConverter.GetBytes(entry.Length);
            
            entrySizeBuffer.CopyTo(buffer, currIdx);
            currIdx += 4;
            
            Encoding.UTF8.GetBytes(entry).CopyTo(buffer, currIdx);
            currIdx += entry.Length;
        }

        await stream.WriteAsync(buffer);
    }

    public override async Task<SetCacheEntry?> Deserialize(Stream stream)
    {
        throw new NotImplementedException();
    }

    public override SetCacheEntry Clone()
        => new() { Key = Key, Value = new HashSet<string>(Value), TimeToLive = TimeToLive };
}