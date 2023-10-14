using System.Collections.Concurrent;
using PyroCache.Entries;

namespace PyroCache;

public sealed class PyroCache
{
    public ConcurrentDictionary<string, CacheEntryBase> Items { get; } = new();

    public bool Has(string key) => Items.ContainsKey(key);

    public bool TryGet<TCacheEntry>(string key,
        out TCacheEntry? cacheEntry)
        where TCacheEntry : CacheEntryBase
    {
        var success = Items.TryGetValue(key, out var item);
        cacheEntry = item as TCacheEntry ?? default;

        return success;
    }

    public bool TryAdd(string key,
        CacheEntryBase entryBase)
        => Items.TryAdd(key, entryBase);

    public CacheEntryBase Set(string key, CacheEntryBase entryBase)
        => Items.AddOrUpdate(key, key => entryBase, (key,
            old) => entryBase);

    public bool TryRemove(string key,
        out CacheEntryBase? entry)
        => Items.TryRemove(key, out entry);
}