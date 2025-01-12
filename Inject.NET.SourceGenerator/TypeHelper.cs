using System.Collections.Generic;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class TypeHelper
{
    public static string WriteType(
        INamedTypeSymbol serviceProviderType,
        Dictionary<ISymbol?, ServiceModel[]> dependencies,
        ServiceModel serviceModel,
        Lifetime currentLifetime)
    {
        return WriteType(serviceProviderType, dependencies, [], serviceModel, currentLifetime);
    }
    
    public static string WriteType(
        INamedTypeSymbol serviceProviderType,
        Dictionary<ISymbol?, ServiceModel[]> dependencies,
        Dictionary<ISymbol?, ServiceModel[]> parentDependencies,
        ServiceModel serviceModel,
        Lifetime currentLifetime)
    {
        if (serviceModel.Lifetime > currentLifetime)
        {
            return $"global::Inject.NET.ThrowHelpers.Throw<{serviceModel.ServiceType.GloballyQualified()}>(\"Injecting type {serviceModel.ImplementationType.Name} with a lifetime of {serviceModel.Lifetime} into an object with a lifetime of {currentLifetime} will cause it to also be {currentLifetime}\")";
        }

        if (!dependencies.ContainsKey(serviceModel.ServiceType) &&
            !parentDependencies.ContainsKey(serviceModel.ServiceType))
        {
            return $"global::Inject.NET.ThrowHelpers.Throw<{serviceModel.ServiceType.GloballyQualified()}>(\"No dependency found for {serviceModel.ServiceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}\")";
        }
        
        if(dependencies.ContainsKey(serviceModel.ServiceType))
        {
            return ObjectConstructionHelper.ConstructNewObject(serviceProviderType, dependencies, serviceModel, currentLifetime);
        }

        return ObjectConstructionHelper.ConstructNewObject(serviceProviderType, parentDependencies, serviceModel, currentLifetime);
    }
}