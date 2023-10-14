using PyroCache.Entries;
using PyroCache.Extensions;
using PyroCache.Filters;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands;

public abstract class BasePyroCommand : IAsyncCommand<StringPackageInfo>
{
    protected readonly PyroCache _cache;
    
    protected static readonly ReadOnlyMemory<char> Nil =
        new(new[] { 'n', 'i', 'l' });
    
    protected static readonly ReadOnlyMemory<char> Zero =
        new(new[] { '0' });

    protected BasePyroCommand(PyroCache cache) => _cache = cache;

    public async ValueTask ExecuteAsync(
        IAppSession session,
        StringPackageInfo package)
    {
        if (session[ValidateCommandFilterAttribute.ErrorKey] is string error)
        {
            await session.SendStringAsync(error);
            return;
        }

        await ExecuteCoreAsync(session, package);
    }

    protected abstract ValueTask ExecuteCoreAsync(
        IAppSession session,
        StringPackageInfo package);

    protected static void SetItemForPurging(IAppSession session,
        CacheEntryBase cacheEntry)
    {
        var itemsToBePurged = (session[CacheEntryPurgerFilterAttribute.ItemsToBePurgedKey] ??=
            new HashSet<string>()) as HashSet<string>;
        itemsToBePurged!.Add(cacheEntry.Key);
        session[CacheEntryPurgerFilterAttribute.ItemsToBePurgedKey] = itemsToBePurged;
    }
}