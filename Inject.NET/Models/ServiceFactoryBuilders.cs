using System.Diagnostics.CodeAnalysis;
using Inject.NET.Enums;
using Inject.NET.Interfaces;

namespace Inject.NET.Models;

public record ServiceFactoryBuilders
{
    public List<IServiceDescriptor> Descriptors { get; } = [];

    public void Add<TService, TImplementation>(Lifetime lifetime, Func<IServiceScope, Type, object> factory)
    {
        Descriptors.Add(new ServiceDescriptor
        {
            ServiceType = typeof(TService),
            ImplementationType = typeof(TImplementation),
            Lifetime = lifetime,
            Factory = factory
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

    public void Add<TService, TImplementation>(Lifetime lifetime, string key, Func<IServiceScope, Type, object> factory)
    {
        Descriptors.Add(new ServiceDescriptor
        {
            ServiceType = typeof(TService),
            ImplementationType = typeof(TImplementation),
            Key = key,
            Lifetime = lifetime,
            Factory = factory
        });
    }
    
    public void AddOpenGeneric([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, Lifetime lifetime, string key)
    {
        Descriptors.Add(new OpenGenericServiceDescriptor
        {
            ServiceType = serviceType,
            ImplementationType = implementationType,
            Key = key,
            Lifetime = lifetime
        });
    }
}