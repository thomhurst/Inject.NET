using System.Reflection;
using Inject.NET.Attributes;
using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET;

internal static class Constructor
{
    private static readonly Dictionary<Type, (Type Type, string? Key)[]> ParameterTypesAndKeys = new();
    
    public static object Construct(IServiceScope serviceScope, Type type, IServiceDescriptor descriptor)
    {
        if (descriptor is ServiceDescriptor serviceDescriptor)
        {
            return serviceDescriptor.Factory(serviceScope, type);
        }
        
        if (descriptor is OpenGenericServiceDescriptor openGenericServiceDescriptor)
        {
            var requestedTypeTypeArguments = type.GenericTypeArguments;

            var newType = openGenericServiceDescriptor.ImplementationType.MakeGenericType(requestedTypeTypeArguments);

            if (!ParameterTypesAndKeys.TryGetValue(newType, out var parameterTypesAndKeys))
            {
                ParameterTypesAndKeys[newType] = parameterTypesAndKeys = newType
                    .GetConstructors()
                    .FirstOrDefault(x => !x.IsStatic)
                    ?.GetParameters()
                    .Select(x => (Type: x.ParameterType, Key: GetParameterKey(x)))
                    .ToArray() ?? [];
            }

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