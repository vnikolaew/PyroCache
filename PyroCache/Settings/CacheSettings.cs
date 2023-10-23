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