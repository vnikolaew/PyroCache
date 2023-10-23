using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PyroCache.Settings;

namespace PyroCache.Jobs;

internal sealed class CachePersisterWorker : BackgroundService
{
    private readonly CacheSettings _cacheSettings;

    private readonly ILogger<CachePersisterWorker> _logger;

    private const string DataFileName = "pyro.db";

    private readonly PyroCache _pyroCache;

    private readonly PeriodicTimer _periodicTimer;

    private readonly List<PeriodicTimer> _saveTimers;
    
    private readonly IHostEnvironment _environment;

    public CachePersisterWorker(
        CacheSettings cacheSettings,
        PyroCache pyroCache,
        ILogger<CachePersisterWorker> logger,
        IHostEnvironment environment)
    {
        _cacheSettings = cacheSettings;
        _pyroCache = pyroCache;
        _logger = logger;
        _saveTimers = cacheSettings.Save
            .Select(s => new PeriodicTimer(TimeSpan.FromSeconds(s)))
        _environment = environment;
        _periodicTimer = new PeriodicTimer(
            TimeSpan.FromSeconds(_cacheSettings.FlushIntervalSeconds ?? 10));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var dataFileName = Path.Combine(
            _environment.ContentRootPath,
            _cacheSettings.DataDirectory, DataFileName);

        // First load initial Cache data:
        await using var readStream = File.OpenWrite(dataFileName);
        await _pyroCache.Deserialize(readStream);
        _logger.LogInformation("Loaded {ItemCount} items from DB file.", _pyroCache.Items.Count);

        while (await _periodicTimer.WaitForNextTickAsync(stoppingToken))
        {
            await using var fileStream = File.OpenWrite(dataFileName);
            await _pyroCache.Serialize(fileStream);
            _logger.LogInformation("Database flushed at [{DateTime:u}].", DateTimeOffset.Now);
        }
    }
}