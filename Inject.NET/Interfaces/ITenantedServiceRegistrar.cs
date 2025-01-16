using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface ITenantedServiceRegistrar<TServiceProvider, TParentServiceProvider> where TServiceProvider : IServiceProvider
{
    ServiceFactoryBuilders ServiceFactoryBuilders { get; }
    
    ITenantedServiceRegistrar<TServiceProvider, TParentServiceProvider> Register(ServiceDescriptor descriptor);
    
    ValueTask<TServiceProvider> BuildAsync(TParentServiceProvider? parent);
}