using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

public static class TypeExtensions
{
    public static bool IsGenericDefinition(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol is ITypeParameterSymbol)
        {
            return true;
        }

        if (SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, typeSymbol))
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

        return namedTypeSymbol.TypeArguments.Any(a => a.IsGenericDefinition());
    } 
    
    public static string GloballyQualified(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
    
    public static string GloballyQualifiedNonGeneric(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);
}