using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class TypeHelper
{
    public static string GetOrConstructType(
        INamedTypeSymbol serviceProviderType,
        IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies,
        ServiceModel serviceModel,
        Lifetime currentLifetime)
    {
        if (serviceModel.Lifetime > currentLifetime)
        {
            return
                $"global::Inject.NET.ThrowHelpers.Throw<{serviceModel.ServiceType.GloballyQualified()}>(\"Injecting type {serviceModel.ImplementationType.Name} with a lifetime of {serviceModel.Lifetime} into an object with a lifetime of {currentLifetime} will cause it to also be {currentLifetime}\")";
        }

        if (!dependencies.Keys.Select(k => k.Type).Contains(serviceModel.ServiceType, SymbolEqualityComparer.Default))
        {
            return
                $"global::Inject.NET.ThrowHelpers.Throw<{serviceModel.ServiceType.GloballyQualified()}>(\"No dependency found for {serviceModel.ServiceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}\")";
        }

        if (serviceModel.Lifetime == Lifetime.Singleton)
        {
            if (serviceModel.ResolvedFromParent)
            {
                return $"Singletons.ParentScope.{PropertyNameHelper.Format(serviceModel)}";
            }

            return $"Singletons.{PropertyNameHelper.Format(serviceModel)}";
        }

        if (serviceModel.Lifetime == Lifetime.Scoped)
        {
            if (serviceModel.ResolvedFromParent)
            {
                return $"ParentScope.{PropertyNameHelper.Format(serviceModel)}";
            }

            return $"{PropertyNameHelper.Format(serviceModel)}";
        }

        if (serviceModel.Lifetime == Lifetime.Transient)
        {
            return ObjectConstructionHelper.ConstructNewObject(serviceProviderType, dependencies,
                serviceModel, currentLifetime);
        }

        throw new ArgumentOutOfRangeException();
    }
}