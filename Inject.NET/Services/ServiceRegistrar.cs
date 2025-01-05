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

    public ITenantedServiceRegistrar Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Func<IServiceScope, Type, T> factory, Lifetime lifetime)
    {
        ServiceFactoryBuilders.Add(typeof(T), lifetime, factory);

        return this;
    }

    public ITenantedServiceRegistrar RegisterOpenGeneric([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, Lifetime lifetime)
    {
        ServiceFactoryBuilders.AddOpenGeneric(serviceType, implementationType, lifetime);

        return this;
    }

    public ITenantedServiceRegistrar RegisterKeyed<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Func<IServiceScope, Type, string, T> factory, Lifetime lifetime, string key)
    {
        ServiceFactoryBuilders.Add(typeof(T), lifetime, key, factory);

        return this;
    }

    public ITenantedServiceRegistrar RegisterKeyedOpenGeneric([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, Lifetime lifetime,
        string key)
    {
        ServiceFactoryBuilders.AddOpenGeneric(serviceType, implementationType, lifetime, key);

        return this;
    }

    public OnBeforeTenantBuild OnBeforeBuild { get; set; } = _ => { };

    public async Task<ITenantedServiceProvider> BuildAsync()
    {
        OnBeforeBuild(this);

        var serviceProvider = new ServiceProvider(ServiceFactoryBuilders.AsReadOnly(), _tenants);
        
        await serviceProvider.InitializeAsync();
        
        return serviceProvider;
    }

    public IServiceRegistrar GetOrCreateTenant(string tenantId)
    {
        return _tenants.GetOrAdd(tenantId, new TenantServiceRegistrar());
    }
}