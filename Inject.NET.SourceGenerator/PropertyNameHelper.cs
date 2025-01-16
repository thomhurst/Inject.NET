using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Inject.NET.SourceGenerator;

public class PropertyNameHelper
{
    public static string Format(ServiceModel singleton)
    {
        return Format(singleton.ServiceType);
    }

    public static string Format(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
            .Replace('<', '_')
            .Replace('>', '_')
            .Replace(".", "__")
            .Replace("?", string.Empty);
    }
}