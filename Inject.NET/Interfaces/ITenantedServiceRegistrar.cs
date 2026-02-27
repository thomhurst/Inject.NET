using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface ITenantedServiceRegistrar<TServiceProvider, TParentServiceProvider> : IServiceRegistrar
    where TServiceProvider : IServiceProvider
{
    new ITenantedServiceRegistrar<TServiceProvider, TParentServiceProvider> Register(ServiceDescriptor descriptor);

    ValueTask<TServiceProvider> BuildAsync(TParentServiceProvider? parent);
}