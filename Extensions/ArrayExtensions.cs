namespace PyroCache.Extensions;

public static class ArrayExtensions
{
    public static int IndexOf<T>(
        this T[] array,
        Predicate<T> predicate)
        => Array.FindIndex(array,predicate);
}