namespace Inject.NET.Helpers;

public static class TypeHelper
{
    public static bool IsEnumerable<T>(Type type)
    {
        return type == typeof(IEnumerable<T>) || type.GetInterfaces().Any(x => x == typeof(IEnumerable<T>));
    }
}