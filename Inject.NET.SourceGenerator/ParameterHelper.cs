using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using INamedTypeSymbol = Microsoft.CodeAnalysis.INamedTypeSymbol;
using ISymbol = Microsoft.CodeAnalysis.ISymbol;
using ITypeParameterSymbol = Microsoft.CodeAnalysis.ITypeParameterSymbol;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Inject.NET.SourceGenerator;

internal static class ParameterHelper
{
    public static IEnumerable<string> BuildParameters(INamedTypeSymbol serviceProviderType,
        IDictionary<ISymbol?, List<ServiceModel>> dependencies,
        ServiceModel serviceModel, Lifetime currentLifetime)
    {
        foreach (var parameter in serviceModel.Parameters)
        {
            if (WriteParameter(serviceProviderType, dependencies, parameter, serviceModel, currentLifetime) is { } written)
            {
                yield return written;
            }
        }
    }

    public static string? WriteParameter(INamedTypeSymbol serviceProviderType,
        IDictionary<ISymbol?, List<ServiceModel>> dependencies,
        Parameter parameter,
        ServiceModel serviceModel,
        Lifetime currentLifetime)
    {
        List<ServiceModel>? models = null;
        
        if (parameter.Type is ITypeParameterSymbol typeParameterSymbol)
        {
            var substitutedTypeIndex = serviceModel.ServiceType.TypeParameters.ToList()
                .FindIndex(x => x.Name == typeParameterSymbol.Name);

            if (substitutedTypeIndex != -1)
            {
                var subtitutedType = serviceModel.ServiceType.TypeArguments[substitutedTypeIndex];

                if (!dependencies.TryGetValue(subtitutedType, out models))
                {
                    var key = parameter.Key is null ? "null" : $"\"{parameter.Key}\"";

                    if (serviceModel.ResolvedFromParent)
                    {
                        return parameter.IsOptional
                            ? $"ParentScope.GetOptionalService<{subtitutedType.GloballyQualified()}>({key})"
                            : $"ParentScope.GetRequiredService<{subtitutedType.GloballyQualified()}>({key})"; 
                    }
                    
                    return parameter.IsOptional
                        ? $"scope.GetOptionalService<{subtitutedType.GloballyQualified()}>({key})"
                        : $"scope.GetRequiredService<{subtitutedType.GloballyQualified()}>({key})";
                }
            }
        }

        if (models is null && !dependencies.TryGetValue(parameter.Type, out models))
        {
            if (parameter.Type is not INamedTypeSymbol { IsGenericType: true } genericType
                || !dependencies.TryGetValue(genericType.ConstructUnboundGenericType(), out models))
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
            if(serviceModel.ResolvedFromParent)
            {
                return $"ParentScope.GetServices<{parameter.Type.GloballyQualified()}>({parameter.Key})";
            }

            return $"scope.GetServices<{parameter.Type.GloballyQualified()}>({parameter.Key})";
        }

        var lastModel = models.Last();
        
        return TypeHelper.GetOrConstructType(serviceProviderType, dependencies, lastModel, currentLifetime);
    }
}