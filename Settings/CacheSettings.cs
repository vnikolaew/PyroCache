namespace PyroCache.Settings;

internal sealed class CacheSettings
{
    public string DataDirectory { get; set; } = default!;

    public int FlushIntervalSeconds { get; set; }
}