using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Inject.NET.SourceGenerator;

public class PropertyNameHelper
{
    public static string Format(ServiceModel serviceModel)
    {
        var propertyName = Format(serviceModel.ServiceType);

        return serviceModel.Key != null ? $"Keyed__{propertyName}__{serviceModel.Key}" : propertyName;
    }

    private static string Format(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
            .Replace('<', '_')
            .Replace('>', '_')
            .Replace(".", "__")
            .Replace("?", string.Empty);
    }
}