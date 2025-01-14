using Inject.NET.Delegates;

namespace Inject.NET.Services;

public abstract class TenantServiceRegistrar<TSelf, TServiceProvider, TSingletonScope, TRootServiceProvider, TDefaultSingletonScope, TDefaultScope> : ServiceRegistrar<TServiceProvider>
    where TSelf : TenantServiceRegistrar<TSelf, TServiceProvider, TSingletonScope, TRootServiceProvider, TDefaultSingletonScope, TDefaultScope>
    where TServiceProvider : TenantServiceProvider<TSingletonScope, TRootServiceProvider, TDefaultSingletonScope, TDefaultScope>
    where TSingletonScope : TenantedSingletonScope<TSingletonScope, TRootServiceProvider, TDefaultSingletonScope, TDefaultScope>
    where TDefaultSingletonScope : SingletonScope
    where TRootServiceProvider : ServiceProviderRoot<TRootServiceProvider, TDefaultSingletonScope>
    where TDefaultScope : ServiceScope<TRootServiceProvider, TDefaultSingletonScope>
{
    public new OnBeforeTenantBuild<TSelf, TServiceProvider> OnBeforeBuild { get; set; } = _ => { };
}