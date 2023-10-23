using System.Collections.Concurrent;
using System.Text;
using PyroCache.Entries;

namespace PyroCache;

public sealed class PyroCache
{
    public ConcurrentDictionary<string, ICacheEntry> Items { get; private set; } = new();

    public IEnumerable<KeyValuePair<string, TEntry>> Entries<TEntry>(
        Func<TEntry, bool>? predicate = default)
        where TEntry : ICacheEntry
        => Items
            .Where(e => e.Value is TEntry entry && (predicate?.Invoke(entry) ?? true))
            .Cast<KeyValuePair<string, TEntry>>();

    public async Task Serialize(Stream stream)
    {
        // Write Items' size first:
        var sizeBuffer = new byte[4];
        BitConverter.GetBytes(Items.Count).CopyTo(sizeBuffer, 0);
        await stream.WriteAsync(sizeBuffer);

        foreach (var (key, value) in Items)
        {
            var keyBuffer = Encoding.UTF8.GetBytes(key);
            var keySizeBuffer = BitConverter.GetBytes(keyBuffer.Length);

            stream.Write(keySizeBuffer);
            stream.Write(keyBuffer);
            await value.Serialize(stream);
        }
    }

    public async Task Deserialize(Stream stream)
    {
        // Read Items' size first:
        var sizeBuffer = new byte[4];
        await stream.ReadExactlyAsync(sizeBuffer);
        var size = BitConverter.ToInt64(sizeBuffer);

        Items = new ConcurrentDictionary<string, ICacheEntry>();
        for (int i = 0; i < size; i++)
        {
            var keySizeBuffer = new byte[4];
            await stream.ReadExactlyAsync(keySizeBuffer);

            var keySize = BitConverter.ToInt64(keySizeBuffer);
            var keyBuffer = new byte[keySize];
            await stream.ReadExactlyAsync(keyBuffer);

            var key = Encoding.UTF8.GetString(keyBuffer);

            var entryType = (CacheEntryType)stream.ReadByte();
            ICacheEntry entry = entryType switch
            {
                CacheEntryType.Channel => new ChannelCacheEntry { Key = key },
                CacheEntryType.Geospatial => new GeospatialIndexCacheEntry { Key = key },
                CacheEntryType.Hash => new HashCacheEntry { Key = key },
                CacheEntryType.List => new ListCacheEntry { Key = key },
                CacheEntryType.Set => new SetCacheEntry { Key = key },
                CacheEntryType.SortedSet => new SortedSetCacheEntry { Key = key },
                CacheEntryType.String => new StringCacheEntry { Key = key, Value = string.Empty },
                _ => null!
            };

            Items.TryAdd(key, (ICacheEntry)entry.DeserializeI(stream));
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

    public bool TryAdd(string key,
        ICacheEntry entryBase)
        => Items.TryAdd(key, entryBase);

    public ICacheEntry Set(string key,
        ICacheEntry entryBase)
        => Items.AddOrUpdate(key, key => entryBase, (key,
            old) => entryBase);

    public bool TryRemove(string key,
        out ICacheEntry? entry)
        => Items.TryRemove(key, out entry);
}