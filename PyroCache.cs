using System.Collections.Concurrent;
using System.Text;
using PyroCache.Entries;

namespace PyroCache;

public sealed class PyroCache
{
    public ConcurrentDictionary<string, ICacheEntry> Items { get; } = new();

    public async Task Serialize(Stream stream)
    {
        foreach (var (key, value) in Items)
        {
            var keyBuffer = Encoding.UTF8.GetBytes(key);
            var keySizeBuffer = BitConverter.GetBytes(keyBuffer.Length);

            stream.Write(keySizeBuffer);
            stream.Write(keyBuffer);
            await value.Serialize(stream);
        }
    }
    
    public bool Has(string key) => Items.ContainsKey(key);

    public bool TryGet<TCacheEntry>(string key,
        out TCacheEntry? cacheEntry)
        where TCacheEntry : class, ICacheEntry
    {
        var success = Items.TryGetValue(key, out var item);
        cacheEntry = item as TCacheEntry ?? default;

        return success;
    }

    public bool TryAdd(string key, ICacheEntry entryBase)
        => Items.TryAdd(key, entryBase);

    public ICacheEntry Set(string key, ICacheEntry entryBase)
        => Items.AddOrUpdate(key, key => entryBase, (key,
            old) => entryBase);

    public bool TryRemove(string key, out ICacheEntry? entry)
        => Items.TryRemove(key, out entry);
}