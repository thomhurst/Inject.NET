using System.Collections.Generic;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class TypeHelper
{
    public static string WriteType(
        INamedTypeSymbol serviceProviderType,
        Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary,
        ServiceModel serviceModel,
        Lifetime currentLifetime)
    {
        if (serviceModel.Lifetime > currentLifetime)
        {
            return $"global::Inject.NET.ThrowHelpers.Throw<{serviceModel.ServiceType.GloballyQualified()}>(\"Injecting type {serviceModel.ImplementationType.Name} with a lifetime of {serviceModel.Lifetime} into an object with a lifetime of {currentLifetime} will cause it to also be {currentLifetime}\")";
        }
        
        if (serviceModel.Lifetime == Lifetime.Transient)
        {
            return ObjectConstructionHelper.ConstructNewObject(serviceProviderType, dependencyDictionary, serviceModel, currentLifetime);
        }

        if (serviceModel.Lifetime == Lifetime.Singleton)
        {
            return $"(({serviceProviderType.GloballyQualified()}SingletonScope)scope.SingletonScope).{PropertyNameHelper.Format(serviceModel)}.Value";
        }
        
        return $"(({serviceProviderType.GloballyQualified()}Scope)scope).{PropertyNameHelper.Format(serviceModel)}.Value";
    }
}