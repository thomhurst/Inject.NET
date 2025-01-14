using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface ITenantedServiceRegistrar<TServiceProvider> where TServiceProvider : IServiceProvider
{
    ServiceFactoryBuilders ServiceFactoryBuilders { get; }
    
    ITenantedServiceRegistrar<TServiceProvider> Register(ServiceDescriptor descriptor);
    
    ValueTask<TServiceProvider> BuildAsync();
}