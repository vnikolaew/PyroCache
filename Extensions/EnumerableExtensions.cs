using System.Threading.Channels;

namespace PyroCache.Extensions;

public static class EnumerableExtensions
{
    public static string Join(
        this IEnumerable<string> values,
        string symbol)
        => string.Join(symbol, values);

    public static IAsyncEnumerable<T> Merge<T>(this IEnumerable<IAsyncEnumerable<T>> streams)
    {
        var channel = Channel.CreateUnbounded<T>();
        foreach (var stream in streams)
        {
            Task.Run(async () =>
            {
                await foreach (var item in stream)
                {
                    await channel.Writer.WriteAsync(item);
                }
            });
        }

        return channel.Reader.ReadAllAsync();
    }
}