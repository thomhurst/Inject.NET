namespace Inject.NET.Extensions;

public static class TypeExtensions
{
    public static bool IsIEnumerable(this Type type)
    {
        return type.IsGenericType
               && typeof(IEnumerable<>).IsAssignableFrom(type.MakeGenericType());
    }
}