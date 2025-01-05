using System.Reflection;
using Inject.NET.Attributes;
using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET;

internal static class Constructor
{
    public static object Construct(IServiceScope serviceScope, Type type, string? key, IServiceDescriptor descriptor)
    {
        if (descriptor is ServiceDescriptor serviceDescriptor)
        {
            return serviceDescriptor.Factory(serviceScope, type);
        }

        if (descriptor is KeyedServiceDescriptor keyedServiceDescriptor)
        {
            return keyedServiceDescriptor.Factory(serviceScope, type, key!);
        }
        
        if (descriptor is OpenGenericServiceDescriptor openGenericServiceDescriptor)
        {
            var requestedTypeTypeArguments = type.GenericTypeArguments;

            var newType = openGenericServiceDescriptor.ImplementationType.MakeGenericType(requestedTypeTypeArguments);

            var parameterTypesAndKeys = newType
                .GetConstructors()
                .FirstOrDefault(x => !x.IsStatic)
                ?.GetParameters()
                .Select(x => (Type: x.ParameterType, Key: GetParameterKey(x))) ?? [];

            var constructedParameters = parameterTypesAndKeys
                        .Select(tuple =>
                        {
                            if (tuple.Key is null)
                            {
                                return serviceScope.GetService(type);
                            }

                            return serviceScope.GetService(type, tuple.Key);
                        })
                        .ToArray();
            
            return Activator.CreateInstance(newType, constructedParameters) ?? throw new ArgumentNullException(nameof(newType));
        }
        
        if (descriptor is OpenGenericKeyedServiceDescriptor openGenericKeyedServiceDescriptor)
        {
            var requestedTypeTypeArguments = type.GenericTypeArguments;

            var newType = openGenericKeyedServiceDescriptor.ImplementationType.MakeGenericType(requestedTypeTypeArguments);

            var parameterTypesAndKeys = newType
                .GetConstructors()
                .FirstOrDefault(x => !x.IsStatic)
                ?.GetParameters()
                .Select(x => (Type: x.ParameterType, Key: GetParameterKey(x))) ?? [];

            var constructedParameters = parameterTypesAndKeys
                .Select(tuple =>
                {
                    if (tuple.Key is null)
                    {
                        return serviceScope.GetService(type);
                    }

                    return serviceScope.GetService(type, tuple.Key);
                })
                .ToArray();
            
            return Activator.CreateInstance(newType, constructedParameters) ?? throw new ArgumentNullException(nameof(newType));
        }

        throw new ArgumentOutOfRangeException(nameof(descriptor));
    }

    private static string? GetParameterKey(ParameterInfo x)
    {
        var serviceKeyAttribute = x.GetCustomAttributes(typeof(ServiceKeyAttribute)).FirstOrDefault() as ServiceKeyAttribute;

        return serviceKeyAttribute?.Key;
    }
}