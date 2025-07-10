namespace MapLib.Util;

public static class CollectionExtensions
{
    /// <summary>
    /// Applies the given action to each item.
    /// </summary>
    public static void Each<T>(this IEnumerable<T> items, Action<T> action)
    {
        foreach (T item in items)
            action(item);
    }
}