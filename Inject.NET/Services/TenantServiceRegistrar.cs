using System.Diagnostics.CodeAnalysis;
using Inject.NET.Delegates;
using Inject.NET.Enums;
using Inject.NET.Extensions;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public class TenantServiceRegistrar : IServiceRegistrar
{
    public ServiceFactoryBuilders ServiceFactoryBuilders { get; } = new();
    
    public IServiceRegistrar Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Func<IServiceScope, Type, T> factory, Lifetime lifetime)
    {
        ServiceFactoryBuilders.Add(typeof(T), lifetime, factory);

        return this;
    }

    public IServiceRegistrar RegisterOpenGeneric([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, Lifetime lifetime)
    {
        if (implementationType.IsAssignableTo(serviceType))
        {
            throw new ArgumentException($"The implementation type {implementationType} is not assignable to {serviceType}");
        }
        
        ServiceFactoryBuilders.AddOpenGeneric(serviceType, implementationType, lifetime);

        return this;
    }

    public IServiceRegistrar RegisterKeyed<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Func<IServiceScope, Type, string, T> factory, Lifetime lifetime, string key)
    {
        ServiceFactoryBuilders.Add(typeof(T), lifetime, key, factory);

        return this;
    }

    public IServiceRegistrar RegisterKeyedOpenGeneric([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, Lifetime lifetime,
        string key)
    {
        if (implementationType.IsAssignableTo(serviceType))
        {
            throw new ArgumentException($"The implementation type {implementationType} is not assignable to {serviceType}");
        }
        
        ServiceFactoryBuilders.AddOpenGeneric(serviceType, implementationType, lifetime, key);

        return this;
    }

    public OnBeforeBuild OnBeforeBuild { get; set; } = _ => { };
    
    public async Task<IServiceProvider> BuildAsync(IServiceProvider rootServiceProvider)
    {
        OnBeforeBuild(this);

        var serviceProvider = new TenantServiceProvider((ServiceProviderRoot)rootServiceProvider, ServiceFactoryBuilders.AsReadOnly());
        
        await serviceProvider.InitializeAsync();
        
        return serviceProvider;
    }
}