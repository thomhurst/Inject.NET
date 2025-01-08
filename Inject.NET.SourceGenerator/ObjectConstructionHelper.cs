using System.Collections.Generic;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class ObjectConstructionHelper
{
    public static string ConstructNewObject(INamedTypeSymbol serviceProviderType, Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary, ServiceModel serviceModel)
    {
        var lastTypeInDictionary = dependencyDictionary[serviceModel.ServiceType][^1];
        
        if (!serviceModel.IsOpenGeneric)
        {
            return
                $"new {lastTypeInDictionary.ImplementationType.GloballyQualified()}({string.Join(", ", ParameterHelper.BuildParameters(serviceProviderType, dependencyDictionary, serviceModel))})";
        }
        
        return $$"""
                  Activator.CreateInstance(typeof({{lastTypeInDictionary.ImplementationType.GloballyQualified()}}).MakeGenericType(type.GenericTypeArguments), [ ..type.GenericTypeArguments.Select(x => scope.GetService(x)) ])
                 """;
    }
}