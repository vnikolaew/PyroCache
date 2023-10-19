﻿namespace PyroCache.Entries;

public abstract class CacheEntryBase<TEntry> : ICacheEntry
{
    public required string Key { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    public DateTimeOffset LastAccessedAt { get; set; } = DateTimeOffset.Now;

    public TimeSpan? TimeToLive { get; set; }

    public abstract Task Serialize(Stream stream);

    public abstract Task<TEntry?> Deserialize(Stream stream);

    public bool IsExpired
        => TimeToLive is not null && DateTimeOffset.Now > CreatedAt + TimeToLive;
}

public interface ICacheEntry
{
    public string Key { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastAccessedAt { get; set; }

    public TimeSpan? TimeToLive { get; set; }
    
    public Task Serialize(Stream stream);

    // public Task<TEntry?> Deserialize(Stream stream);
}