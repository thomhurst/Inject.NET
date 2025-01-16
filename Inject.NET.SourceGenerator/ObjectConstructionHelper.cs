using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class ObjectConstructionHelper
{
    public static string ConstructNewObject(INamedTypeSymbol serviceProviderType, Dictionary<ISymbol?, ServiceModel[]> dependencies, Dictionary<ISymbol?, ServiceModel[]> parentDependencies, ServiceModel serviceModel, Lifetime currentLifetime)
    {
        var lastTypeInDictionary = dependencies[serviceModel.ServiceType][^1];

        if (serviceModel.IsOpenGeneric)
        {
            return $" Activator.CreateInstance(typeof({lastTypeInDictionary.ImplementationType.GloballyQualified()}).MakeGenericType(type.GenericTypeArguments), [ ..type.GenericTypeArguments.Select(x => scope.GetService(x)) ])";
        }
        
        if (!dependencies.ContainsKey(serviceModel.ServiceType) &&
            !parentDependencies.ContainsKey(serviceModel.ServiceType))
        {
            return
                $"global::Inject.NET.ThrowHelpers.Throw<{serviceModel.ServiceType.GloballyQualified()}>(\"No dependency found for {serviceModel.ServiceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}\")";
        }

        return
            $"new {lastTypeInDictionary.ImplementationType.GloballyQualified()}({string.Join(", ", ParameterHelper.BuildParameters(serviceProviderType, dependencies, parentDependencies, serviceModel, currentLifetime))})";
    }
}