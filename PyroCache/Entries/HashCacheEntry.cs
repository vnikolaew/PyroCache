using System.Text;

namespace PyroCache.Entries;

public class HashCacheEntry : CacheEntryBase<HashCacheEntry>
{
    public Dictionary<string, byte[]> Value { get; init; } = new();

    public IEnumerable<string> Keys => Value.Keys;

    public IEnumerable<byte[]> Values => Value.Values;

    public int Length => Value.Count;

    public bool FieldExists(string key)
        => Value.ContainsKey(key);

    public void Set(string key,
        byte[] value)
        => Value[key] = value;

    public bool IncrementBy(string key, int amount, out int? newValue)
    {
        if (!Value.TryGetValue(key, out var value))
        {
            newValue = default;
            return false;
        }

        if (!int.TryParse(Encoding.UTF8.GetString(value), out var integer))
        {
            newValue = default;
            return false;
        }

        integer += amount;
        Value[key] = Encoding.UTF8.GetBytes(integer.ToString());

        newValue = integer;
        return true;
    }

    public byte[]? Get(string key)
        => Value.TryGetValue(key, out var value) ? value : default;

    public int Delete(IEnumerable<string> keys)
        => keys
            .Select(key => Value.Remove(key))
            .Count(_ => _);

    public void MultiSet(IEnumerable<(string key, byte[] value)> entries)
    {
        foreach (var (key, value) in entries)
        {
            Value[key] = value;
        }
    }

    public IEnumerable<byte[]?> MultiGet(IEnumerable<string> keys)
        => keys.Select(key =>
            Value.TryGetValue(key, out var value) ? value : default);

    public override CacheEntryType EntryType => CacheEntryType.Hash;

    protected override async Task SerializeCore(Stream stream)
    {
        throw new NotImplementedException();
    }

    public override async Task<HashCacheEntry?> Deserialize(Stream stream)
    {
        throw new NotImplementedException();
    }

    public override HashCacheEntry Clone()
        => new() { Key = Key, Value = new Dictionary<string, byte[]>(Value), TimeToLive = TimeToLive };
}