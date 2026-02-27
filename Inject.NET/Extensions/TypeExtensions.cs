namespace Inject.NET.Extensions;

public static class TypeExtensions
{
    private static readonly Type EnumerableGeneric = typeof(IEnumerable<>);
    private static readonly Type LazyGeneric = typeof(Lazy<>);

    public static bool IsIEnumerable(this Type type)
    {
        return type.IsGenericType
               && type.GetGenericTypeDefinition() == EnumerableGeneric;
    }

    public static bool IsLazy(this Type type)
    {
        return type.IsGenericType
               && type.GetGenericTypeDefinition() == LazyGeneric;
    }
}