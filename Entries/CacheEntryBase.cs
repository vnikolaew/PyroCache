namespace PyroCache.Entries;

public abstract class CacheEntryBase
{
    public required string Key { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    public DateTimeOffset LastAccessedAt { get; set; }
    
    public TimeSpan? TimeToLive { get; set; }

    public bool IsExpired
        => TimeToLive is not null && DateTimeOffset.Now > CreatedAt + TimeToLive;
}