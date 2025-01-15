using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

public abstract class ServiceRegistrar<TServiceProvider, TParentServiceProvider> : ITenantedServiceRegistrar<TServiceProvider, TParentServiceProvider> where TServiceProvider : IServiceProvider
{
    public ServiceFactoryBuilders ServiceFactoryBuilders { get; } = new();

    public ITenantedServiceRegistrar<TServiceProvider, TParentServiceProvider> Register(ServiceDescriptor serviceDescriptor)
    {
        ServiceFactoryBuilders.Add(serviceDescriptor);

        return this;
    }
    
    public abstract ValueTask<TServiceProvider> BuildAsync(TParentServiceProvider? parentServiceProvider);
}