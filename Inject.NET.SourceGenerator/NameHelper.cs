using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Inject.NET.SourceGenerator;

public class NameHelper
{
    public static string AsProperty(ServiceModel serviceModel)
    {
        var typeString = serviceModel.ServiceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
            .Replace('<', '_')
            .Replace('>', '_')
            .Replace(".", "__")
            .Replace(',', '_')
            .Replace(" ", "_")
            .Replace("?", string.Empty);
        
        var propertyName = $"{typeString}__{serviceModel.TenantName}__{serviceModel.Index}";

        return serviceModel.Key != null ? $"Keyed__{propertyName}__{serviceModel.Key}" : propertyName;
    }

    public static string AsField(ServiceModel serviceModel)
    {
        var asProperty = AsProperty(serviceModel);
        
        return $"_{asProperty[..1].ToLower()}{asProperty[1..]}";
    }
}