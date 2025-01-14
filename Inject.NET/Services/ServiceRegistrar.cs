using System.Collections.Concurrent;
using Inject.NET.Delegates;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public abstract class ServiceRegistrar<TServiceProvider> : ITenantedServiceRegistrar<TServiceProvider> where TServiceProvider : IServiceProvider
{
    protected readonly ConcurrentDictionary<string, IServiceRegistrar> Tenants = [];

    public ServiceFactoryBuilders ServiceFactoryBuilders { get; } = new();

    public ITenantedServiceRegistrar<TServiceProvider> Register(ServiceDescriptor serviceDescriptor)
    {
        ServiceFactoryBuilders.Add(serviceDescriptor);

        return this;
    }

    public OnBeforeTenantBuild<ITenantedServiceRegistrar<TServiceProvider>, TServiceProvider> OnBeforeBuild { get; set; } = _ => { };

    public abstract ValueTask<TServiceProvider> BuildAsync();
}