using System.Text;
using SuperSocket;

namespace PyroCache.Extensions;

public static class SessionExtensions
{
    public static readonly object CanceledKey = new();

    public static ValueTask SendStringAsync(
        this IAppSession appSession,
        string value)
        => appSession.SendAsync(Encoding.UTF8.GetBytes(value).AsMemory());

    public static void CancelCommand(this IAppSession appSession)
        => appSession[CanceledKey] = true;

    public static bool IsCanceled(this IAppSession appSession)
        => appSession[CanceledKey] is true;
}