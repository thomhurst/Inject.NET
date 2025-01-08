using System.Collections.Concurrent;
using Inject.NET.Delegates;
using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

public abstract class ServiceRegistrar<TServiceProviderRoot> : ITenantedServiceRegistrar<TServiceProviderRoot> where TServiceProviderRoot : IServiceProviderRoot
{
    private readonly ConcurrentDictionary<string, IServiceRegistrar> _tenants = [];

    public ServiceFactoryBuilders ServiceFactoryBuilders { get; } = new();

    public ITenantedServiceRegistrar<TServiceProviderRoot> Register(ServiceDescriptor serviceDescriptor)
    {
        ServiceFactoryBuilders.Add(serviceDescriptor);

        return this;
    }

    public OnBeforeTenantBuild<ITenantedServiceRegistrar<TServiceProviderRoot>, TServiceProviderRoot> OnBeforeBuild { get; set; } = _ => { };

    public abstract ValueTask<TServiceProviderRoot> BuildAsync();

    public IServiceRegistrar GetOrCreateTenant(string tenantId)
    {
        return _tenants.GetOrAdd(tenantId, new TenantServiceRegistrar());
    }
}