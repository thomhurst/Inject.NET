using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public abstract class ServiceRegistrar<TServiceProvider> : ITenantedServiceRegistrar<TServiceProvider> where TServiceProvider : IServiceProvider
{
    public ServiceFactoryBuilders ServiceFactoryBuilders { get; } = new();

    public ITenantedServiceRegistrar<TServiceProvider> Register(ServiceDescriptor serviceDescriptor)
    {
        ServiceFactoryBuilders.Add(serviceDescriptor);

        return this;
    }
    
    public abstract ValueTask<TServiceProvider> BuildAsync();
}