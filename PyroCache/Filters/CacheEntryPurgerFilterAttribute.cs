using Microsoft.Extensions.DependencyInjection;
using SuperSocket.Command;

namespace PyroCache.Filters;

public sealed class CacheEntryPurgerFilterAttribute : AsyncCommandFilterAttribute
{
    public static readonly object ItemsToBePurgedKey = new();
    
    public override ValueTask<bool> OnCommandExecutingAsync(
        CommandExecutingContext commandContext)
        => new(true);

    public override ValueTask OnCommandExecutedAsync(
        CommandExecutingContext commandContext)
    {
        var cache = commandContext.Session.Server.ServiceProvider.GetRequiredService<PyroCache>();
        if (commandContext.Session[ItemsToBePurgedKey]
            is HashSet<string> { Count: > 0 } itemsToBePurged)
        {
            foreach (var (cacheKey, _) in cache.Items.Where(i => itemsToBePurged.Contains(i.Key)))
            {
                cache.TryRemove(cacheKey, out _);
            }
        }
        
        return ValueTask.CompletedTask;
    }
}