using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

public abstract class TenantServiceRegistrar<TSelf, TServiceProvider, TSingletonScope, TRootServiceProvider, TDefaultSingletonScope, TDefaultScope> 
    : IServiceRegistrar<TenantServiceRegistrar<TSelf, TServiceProvider, TSingletonScope, TRootServiceProvider, TDefaultSingletonScope, TDefaultScope>, TRootServiceProvider, TServiceProvider>
    where TSelf : TenantServiceRegistrar<TSelf, TServiceProvider, TSingletonScope, TRootServiceProvider, TDefaultSingletonScope, TDefaultScope>
    where TServiceProvider : TenantServiceProvider<TSingletonScope, TRootServiceProvider, TDefaultSingletonScope, TDefaultScope>
    where TSingletonScope : TenantedSingletonScope<TSingletonScope, TRootServiceProvider, TDefaultSingletonScope, TDefaultScope>
    where TDefaultSingletonScope : SingletonScope
    where TRootServiceProvider : ServiceProviderRoot<TRootServiceProvider, TDefaultSingletonScope>
    where TDefaultScope : ServiceScope<TRootServiceProvider, TDefaultSingletonScope>
{
    public ServiceFactoryBuilders ServiceFactoryBuilders { get; } = new();

    public TenantServiceRegistrar<TSelf, TServiceProvider, TSingletonScope, TRootServiceProvider, TDefaultSingletonScope, TDefaultScope> Register(ServiceDescriptor serviceDescriptor)
    {
        ServiceFactoryBuilders.Add(serviceDescriptor);

        return (TSelf)this;
    }
    
    public abstract ValueTask<TServiceProvider> BuildAsync(TRootServiceProvider rootServiceProvider);
}