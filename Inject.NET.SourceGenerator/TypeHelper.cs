using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class TypeHelper
{
    public static string GetOrConstructType(
        INamedTypeSymbol serviceProviderType,
        Dictionary<ISymbol?, ServiceModel[]> dependencies,
        Dictionary<ISymbol?, ServiceModel[]> parentDependencies,
        ServiceModel serviceModel,
        Lifetime currentLifetime)
    {
        if (serviceModel.Lifetime > currentLifetime)
        {
            return
                $"global::Inject.NET.ThrowHelpers.Throw<{serviceModel.ServiceType.GloballyQualified()}>(\"Injecting type {serviceModel.ImplementationType.Name} with a lifetime of {serviceModel.Lifetime} into an object with a lifetime of {currentLifetime} will cause it to also be {currentLifetime}\")";
        }

        if (!dependencies.Keys.Contains(serviceModel.ServiceType, SymbolEqualityComparer.Default) &&
            !parentDependencies.Keys.Contains(serviceModel.ServiceType, SymbolEqualityComparer.Default))
        {
            return
                $"global::Inject.NET.ThrowHelpers.Throw<{serviceModel.ServiceType.GloballyQualified()}>(\"No dependency found for {serviceModel.ServiceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}\")";
        }
        
        if (!dependencies.Keys.Contains(serviceModel.ServiceType, SymbolEqualityComparer.Default))
        {
            switch (serviceModel.Lifetime)
            {
                case Lifetime.Singleton:
                    return $"Singletons.ParentScope.{PropertyNameHelper.Format(serviceModel)}";
                case Lifetime.Scoped:
                    return $"_parentScope.{PropertyNameHelper.Format(serviceModel)}";
                case Lifetime.Transient:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if(serviceModel.Lifetime != Lifetime.Transient)
        {
            switch (serviceModel.Lifetime)
            {
                case Lifetime.Singleton:
                    return $"Singletons.{PropertyNameHelper.Format(serviceModel)}";
                case Lifetime.Scoped:
                    return PropertyNameHelper.Format(serviceModel);
                case Lifetime.Transient:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return ObjectConstructionHelper.ConstructNewObject(serviceProviderType, dependencies, parentDependencies,
            serviceModel, currentLifetime);
    }
}