using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class TypeExtensions
{
    private static readonly ConcurrentDictionary<ITypeSymbol, string> _globallyQualifiedCache = new(SymbolEqualityComparer.Default);
    private static readonly ConcurrentDictionary<ITypeSymbol, string> _fullyQualifiedCache = new(SymbolEqualityComparer.Default);
    private static readonly ConcurrentDictionary<ITypeSymbol, string> _globallyQualifiedNonGenericCache = new(SymbolEqualityComparer.Default);
    public static bool IsGenericDefinition(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol is ITypeParameterSymbol)
        {
            return true;
        }
        
        if (typeSymbol is not INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
        {
            return false;
        }

        if (namedTypeSymbol.IsUnboundGenericType)
        {
            return true;
        }
        
        if (SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, typeSymbol))
        {
            return true;
        }

        return namedTypeSymbol.TypeArguments.Any(a => a.IsGenericDefinition());
    } 
    
    public static string GloballyQualified(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null) return string.Empty;
        return _globallyQualifiedCache.GetOrAdd(typeSymbol, static t => t.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix));
    }
    
    public static string FullyQualified(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null) return string.Empty;
        return _fullyQualifiedCache.GetOrAdd(typeSymbol, static t => t.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithoutGlobalPrefix));
    }
    
    public static string GloballyQualifiedNonGeneric(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null) return string.Empty;
        return _globallyQualifiedNonGenericCache.GetOrAdd(typeSymbol, static t => t.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix));
    }
}