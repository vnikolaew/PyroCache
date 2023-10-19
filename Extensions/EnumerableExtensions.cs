namespace PyroCache.Extensions;

public static class EnumerableExtensions
{
    public static string Join(
        this IEnumerable<string> values,
        string symbol)
        => string.Join(symbol, values);
}