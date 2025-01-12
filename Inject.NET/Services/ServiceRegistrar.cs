using System.Collections.Concurrent;
using Inject.NET.Delegates;
using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

public abstract class ServiceRegistrar<TServiceProviderRoot, TSingletonScope> : ITenantedServiceRegistrar<TServiceProviderRoot> where TServiceProviderRoot : IServiceProviderRoot where TSingletonScope : IServiceScope
{
    protected readonly ConcurrentDictionary<string, IServiceRegistrar> Tenants = [];

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
        return Tenants.GetOrAdd(tenantId, new TenantServiceRegistrar<TSingletonScope>());
    }
}