namespace PyroCache.Extensions;

public static class ArrayExtensions
{
    public static int IndexOf<T>(
        this T[] array,
        Predicate<T> predicate)
        => Array.FindIndex(array,predicate);

    public static T RandomItem<T>(this T[] array)
        => array[Random.Shared.Next(0, array.Length)];
}