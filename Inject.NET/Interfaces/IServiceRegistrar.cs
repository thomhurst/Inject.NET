using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface IServiceRegistrar<out TSelf, in TRootServiceProvider, TTenantServiceProvider>
where TSelf : IServiceRegistrar<TSelf, TRootServiceProvider, TTenantServiceProvider>
where TRootServiceProvider : IServiceProvider
where TTenantServiceProvider : IServiceProvider
{
    ServiceFactoryBuilders ServiceFactoryBuilders { get; }
    
    TSelf Register(ServiceDescriptor descriptor);
    
    ValueTask<TTenantServiceProvider> BuildAsync(TRootServiceProvider rootServiceProvider);
}