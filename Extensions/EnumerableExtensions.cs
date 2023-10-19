namespace PyroCache.Extensions;

public static class EnumerableExtensions
{
    public static string Join(
        this IEnumerable<string> values,
        string symbol)
        => string.Join(symbol, values);

    public static int IndexOf<T>(
        this T[] array,
        Predicate<T> predicate)
        => Array.IndexOf(array, predicate);
}