using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Inject.NET.Delegates;
using Inject.NET.Enums;
using Inject.NET.Extensions;
using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

public class ServiceRegistrar : ITenantedServiceRegistrar
{
    private readonly ConcurrentDictionary<string, IServiceRegistrar> _tenants = [];

    public ServiceFactoryBuilders ServiceFactoryBuilders { get; } = new();

    public ITenantedServiceRegistrar Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(Func<IServiceScope, Type, object> factory, Lifetime lifetime)
    {
        ServiceFactoryBuilders.Add<TService, TImplementation>(lifetime, factory);

        return this;
    }

    public ITenantedServiceRegistrar RegisterOpenGeneric([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, Lifetime lifetime)
    {
        if (implementationType.IsAssignableTo(serviceType))
        {
            throw new ArgumentException($"The implementation type {implementationType} is not assignable to {serviceType}");
        }
        
        ServiceFactoryBuilders.AddOpenGeneric(serviceType, implementationType, lifetime);

        return this;
    }

    public ITenantedServiceRegistrar RegisterKeyed<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(Func<IServiceScope, Type, object> factory, Lifetime lifetime, string key)
    {
        ServiceFactoryBuilders.Add<TService, TImplementation>(lifetime, key, factory);

        return this;
    }

    public ITenantedServiceRegistrar RegisterKeyedOpenGeneric([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, Lifetime lifetime,
        string key)
    {
        if (implementationType.IsAssignableTo(serviceType))
        {
            throw new ArgumentException($"The implementation type {implementationType} is not assignable to {serviceType}");
        }
        
        ServiceFactoryBuilders.AddOpenGeneric(serviceType, implementationType, lifetime, key);

        return this;
    }

    public OnBeforeTenantBuild OnBeforeBuild { get; set; } = _ => { };

    public async Task<IServiceProviderRoot> BuildAsync()
    {
        OnBeforeBuild(this);

        var serviceProvider = new ServiceProviderRoot(ServiceFactoryBuilders.AsReadOnly(), _tenants);
        
        await serviceProvider.InitializeAsync();
        
        return serviceProvider;
    }

    public IServiceRegistrar GetOrCreateTenant(string tenantId)
    {
        return _tenants.GetOrAdd(tenantId, new TenantServiceRegistrar());
    }
}