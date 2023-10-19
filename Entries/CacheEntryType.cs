namespace PyroCache.Entries;

public enum CacheEntryType : byte
{
    String,
    List,
    Set,
    SortedSet,
    Hash,
}