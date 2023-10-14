using System.Text;
using SuperSocket;

namespace PyroCache.Extensions;

public static class SessionExtensions
{
    public static ValueTask SendStringAsync(
        this IAppSession appSession,
        string value)
        => appSession.SendAsync(Encoding.UTF8.GetBytes(value).AsMemory());
}