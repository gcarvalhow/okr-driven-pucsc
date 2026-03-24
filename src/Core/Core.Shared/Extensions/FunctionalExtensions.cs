namespace Core.Shared.Extensions;

public static class FunctionalExtensions
{
    public static T Tap<T>(this T instance, Action action)
    {
        action();
        return instance;
    }

    public static T Tap<T>(this T instance, Action<T> action)
    {
        action(instance);
        return instance;
    }

    public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
        foreach (T element in collection)
        {
            action(element);
        }
    }
}