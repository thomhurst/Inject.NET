using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

public static class TypeExtensions
{
    public static string GloballyQualified(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
    
    public static string GloballyQualifiedNonGeneric(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);
}