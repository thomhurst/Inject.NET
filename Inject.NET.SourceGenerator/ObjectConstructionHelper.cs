using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class ObjectConstructionHelper
{
    public static string ConstructNewObject(INamedTypeSymbol serviceProviderType, IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies, ServiceModel serviceModel, Lifetime currentLifetime)
    {
        if (!dependencies.TryGetValue(serviceModel.ServiceKey, out _))
        {
            return
                $"this.GetRequiredService<{serviceModel.ServiceType.GloballyQualified()}>()";
        }

        if (serviceModel.IsOpenGeneric)
        {
            return $" Activator.CreateInstance(typeof({serviceModel.ImplementationType.GloballyQualified()}).MakeGenericType(type.GenericTypeArguments), [ ..type.GenericTypeArguments.Select(x => scope.GetService(x)) ])";
        }

        if (serviceModel.HasFactoryMethod)
        {
            return $"{serviceModel.ServiceProviderType!.GloballyQualified()}.{serviceModel.FactoryMethodName}({string.Join(", ", ParameterHelper.BuildParameters(serviceProviderType, dependencies, serviceModel, currentLifetime))})";
        }

        return
            $"new {serviceModel.ImplementationType.GloballyQualified()}({string.Join(", ", ParameterHelper.BuildParameters(serviceProviderType, dependencies, serviceModel, currentLifetime))})";
    }
}