namespace PyroCache.Settings;

internal sealed class CacheSettings
{
    public string DataDirectory { get; set; } = default!;

    public int? FlushIntervalSeconds { get; set; }

    public List<string> Include { get; set; } = new();

    public List<string> Bind { get; set; } = new();

    public int Port { get; set; }

    public string Logfile { get; set; } = default!;

    public int Databases { get; set; }

    public List<string> Save { get; set; } = default!;

    public List<SaveConfiguration> SaveConfigurations
        => Save.Select(s =>
        {
            var parts = s.Split(" ").Select(long.Parse).ToArray();
            return new SaveConfiguration()
            {
                Seconds = parts[0],
                MinChangesAllowed = parts[1]
            };
        }).ToList();

    public string Compression { get; set; } = default!;

    public string DbFileName { get; set; } = default!;

    public string Dir { get; set; } = default!;

    public string RequirePass { get; set; } = default!;

    public int MaxClients { get; set; }

    public int MaxMemory { get; set; }

    public string MaxMemoryPolicy { get; set; } = default!;

    public string AppendOnly { get; set; } = default!;

    public string AppendFileName { get; set; } = default!;

    public string AppendDirName { get; set; } = default!;

    public string AppendFSync { get; set; } = default!;
}

public class SaveConfiguration : IEquatable<SaveConfiguration>
{
    public Guid Id { get; } = Guid.NewGuid();
    
    public long Seconds { get; set; }

    public long MinChangesAllowed { get; set; }

    public bool Equals(SaveConfiguration? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SaveConfiguration)obj);
    }

    public override int GetHashCode() => Id.GetHashCode();
}