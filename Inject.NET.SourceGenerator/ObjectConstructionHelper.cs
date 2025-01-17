using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class ObjectConstructionHelper
{
    public static string ConstructNewObject(INamedTypeSymbol serviceProviderType, IDictionary<ISymbol?, List<ServiceModel>> dependencies, ServiceModel serviceModel, Lifetime currentLifetime)
    {
        if (!dependencies.TryGetValue(serviceModel.ServiceType, out var dependency))
        {
            return
                $"global::Inject.NET.ThrowHelpers.Throw<{serviceModel.ServiceType.GloballyQualified()}>(\"No dependency found for {serviceModel.ServiceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}\")";
        }
        
        var lastTypeInDictionary = dependency[^1];

        if (serviceModel.IsOpenGeneric)
        {
            return $" Activator.CreateInstance(typeof({lastTypeInDictionary.ImplementationType.GloballyQualified()}).MakeGenericType(type.GenericTypeArguments), [ ..type.GenericTypeArguments.Select(x => scope.GetService(x)) ])";
        }

        return
            $"new {lastTypeInDictionary.ImplementationType.GloballyQualified()}({string.Join(", ", ParameterHelper.BuildParameters(serviceProviderType, dependencies, serviceModel, currentLifetime))})";
    }
}