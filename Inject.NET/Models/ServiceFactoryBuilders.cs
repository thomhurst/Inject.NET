using System.Diagnostics.CodeAnalysis;
using Inject.NET.Enums;
using Inject.NET.Interfaces;

namespace Inject.NET.Models;

public record ServiceFactoryBuilders
{
    public List<IServiceDescriptor> Descriptors { get; } = [];
    public List<IKeyedServiceDescriptor> KeyedDescriptors { get; } = [];

    public void Add<T>(Type type, Lifetime lifetime, Func<IServiceScope, Type, T> factory)
    {
        Descriptors.Add(new ServiceDescriptor
        {
            ServiceType = type,
            ImplementationType = typeof(T),
            Lifetime = lifetime,
            Factory = (ss, t) => factory(ss, t)!
        });
    }
    
    public void AddOpenGeneric([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, Lifetime lifetime)
    {
        Descriptors.Add(new OpenGenericServiceDescriptor
        {
            ServiceType = serviceType,
            ImplementationType = implementationType,
            Lifetime = lifetime
        });
    }

    public void Add<T>(Type type, Lifetime lifetime, string key, Func<IServiceScope, Type, string, T> factory)
    {
        KeyedDescriptors.Add(new KeyedServiceDescriptor
        {
            ServiceType = type,
            ImplementationType = typeof(T),
            Key = key,
            Lifetime = lifetime,
            Factory = (ss, t, k) => factory(ss, t, k)!
        });
    }
    
    public void AddOpenGeneric([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, Lifetime lifetime, string key)
    {
        KeyedDescriptors.Add(new OpenGenericKeyedServiceDescriptor
        {
            ServiceType = serviceType,
            ImplementationType = implementationType,
            Key = key,
            Lifetime = lifetime
        });
    }
}