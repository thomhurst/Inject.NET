namespace Inject.NET.Extensions;

public static class TypeExtensions
{
    private static readonly Type EnumerableGeneric = typeof(IEnumerable<>);

    public static bool IsIEnumerable(this Type type)
    {
        return type.IsGenericType
               && type.GetGenericTypeDefinition() == EnumerableGeneric;
    }
}