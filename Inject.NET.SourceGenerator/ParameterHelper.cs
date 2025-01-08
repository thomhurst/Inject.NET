using System.Collections.Generic;
using System.Linq;
using Inject.NET.SourceGenerator.Models;
using INamedTypeSymbol = Microsoft.CodeAnalysis.INamedTypeSymbol;
using ISymbol = Microsoft.CodeAnalysis.ISymbol;
using ITypeParameterSymbol = Microsoft.CodeAnalysis.ITypeParameterSymbol;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Inject.NET.SourceGenerator;

internal static class ParameterHelper
{
    public static IEnumerable<string> BuildParameters(INamedTypeSymbol serviceProviderType, Dictionary<ISymbol?,ServiceModel[]> dependencyDictionary, ServiceModel serviceModel)
    {
        foreach (var parameter in serviceModel.Parameters)
        {
            if (WriteParameter(serviceProviderType, dependencyDictionary, parameter, serviceModel) is { } written)
            {
                yield return written;
            }
        }
    }

    public static string? WriteParameter(
        INamedTypeSymbol serviceProviderType,
        Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary,
        Parameter parameter, 
        ServiceModel serviceModel)
    {
        ServiceModel[]? models = null;
        
        if (parameter.Type is ITypeParameterSymbol typeParameterSymbol)
        {
            var substitutedTypeIndex = serviceModel.ServiceType.TypeParameters.ToList()
                .FindIndex(x => x.Name == typeParameterSymbol.Name);

            if (substitutedTypeIndex != -1)
            {
                var subtitutedType = serviceModel.ServiceType.TypeArguments[substitutedTypeIndex];

                if (!dependencyDictionary.TryGetValue(subtitutedType, out models))
                {
                    var key = parameter.Key is null ? "null" : $"\"{parameter.Key}\"";

                    return parameter.IsOptional
                        ? $"scope.GetOptionalService<{subtitutedType.GloballyQualified()}>({key})"
                        : $"scope.GetRequiredService<{subtitutedType.GloballyQualified()}>({key})";
                }
            }
        }

        if (models is null && !dependencyDictionary.TryGetValue(parameter.Type, out models))
        {
            if (parameter.Type is not INamedTypeSymbol { IsGenericType: true } genericType
                || !dependencyDictionary.TryGetValue(genericType.ConstructUnboundGenericType(), out models))
            {
                if (parameter.IsOptional)
                {
                    return null;
                }

                if (parameter.IsNullable)
                {
                    return "null";
                }

                return $"global::Inject.NET.ThrowHelpers.Throw<{parameter.Type.GloballyQualified()}>(\"No dependency found for {parameter.Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)} when trying to construct {serviceModel.ImplementationType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}\")";
            }
        }

        if (parameter.IsEnumerable)
        {
            return $"scope.GetServices<{parameter.Type.GloballyQualified()}>({parameter.Key})";
        }

        var lastModel = models.Last();
        
        return TypeHelper.WriteType(serviceProviderType, dependencyDictionary, lastModel, parameter);
    }
}