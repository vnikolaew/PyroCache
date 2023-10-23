using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PyroCache.Settings;

namespace PyroCache.Jobs;

internal sealed class CachePersisterWorker : BackgroundService
{
    private readonly CacheSettings _cacheSettings;

    private readonly ILogger<CachePersisterWorker> _logger;

    private const string DataFileName = "pyro.db";

    private string DataFile
        => Path.Combine(_environment.ContentRootPath, _cacheSettings.Dir, DataFileName);

    private readonly PyroCache _pyroCache;

    private readonly PeriodicTimer _periodicTimer;

    private readonly IDictionary<SaveConfiguration, PeriodicTimer> _saveTimers;

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
        _saveTimers = cacheSettings
            .SaveConfigurations
            .ToDictionary(c => c, c => new PeriodicTimer(TimeSpan.FromSeconds(c.Seconds)));

        _environment = environment;
        _periodicTimer = new PeriodicTimer(
            TimeSpan.FromSeconds(_cacheSettings.FlushIntervalSeconds ?? 10));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // First load initial Cache data:
        await using var readStream = File.OpenRead(DataFile);
        if(readStream.Length > 0) await _pyroCache.Deserialize(readStream);
        _logger.LogInformation("Loaded {ItemCount} items from DB file", _pyroCache.Items.Count);

        await RunSaveWorkers(stoppingToken);
        Console.WriteLine();
    }

    private async Task RunSaveWorkers(CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        foreach (var (config, timer) in _saveTimers)
        {
            var task = Task.Factory.StartNew(async () =>
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    // Check if min changes have occured in the cache:
                    var changedItemsCount = _pyroCache.Items
                        .Select(i => i.Value.LastAccessedAt)
                        .Count(lat => lat >= DateTimeOffset.Now - timer.Period);

                    if (changedItemsCount >= config.MinChangesAllowed)
                    {
                        // Perform cache save:
                        await using var fileStream = File.OpenWrite(DataFile);
                        await _pyroCache.Serialize(fileStream);
                        _logger.LogInformation("Database flushed at [{DateTime:u}]", DateTimeOffset.Now);
                    }
                }
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            tasks.Add(task);
        }
        
        await Task.WhenAll(tasks);
    }
}