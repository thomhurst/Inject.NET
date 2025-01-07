using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

public static class TypeExtensions
{
    public static bool IsGenericDefinition(this ITypeSymbol typeSymbol)
    {
        return typeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol
               && (namedTypeSymbol.IsUnboundGenericType
                   || namedTypeSymbol.TypeArguments.SequenceEqual<ISymbol>(namedTypeSymbol.TypeParameters, SymbolEqualityComparer.Default)
                   || namedTypeSymbol.TypeArguments.Any(ta => ta.IsGenericDefinition()));
    } 
    
    public static string GloballyQualified(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
    
    public static string GloballyQualifiedNonGeneric(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);
}