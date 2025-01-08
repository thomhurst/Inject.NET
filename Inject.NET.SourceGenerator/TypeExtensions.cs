using System.Linq;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class TypeExtensions
{
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
    
    public static string GloballyQualified(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
    
    public static string FullyQualified(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithoutGlobalPrefix);
    
    public static string GloballyQualifiedNonGeneric(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);
}