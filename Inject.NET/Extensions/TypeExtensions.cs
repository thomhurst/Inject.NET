namespace Inject.NET.Extensions;

public static class TypeExtensions
{
    private static readonly Type EnumerableGeneric = typeof(IEnumerable<>);
    private static readonly Type LazyGeneric = typeof(Lazy<>);
    private static readonly Type FuncGeneric = typeof(Func<>);

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

    public static bool IsFunc(this Type type)
    {
        return type.IsGenericType
               && type.GetGenericArguments().Length == 1
               && type.GetGenericTypeDefinition() == FuncGeneric;
    }
}