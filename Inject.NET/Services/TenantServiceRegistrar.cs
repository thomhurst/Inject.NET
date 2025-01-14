using Inject.NET.Delegates;
using Inject.NET.Extensions;
using Inject.NET.Interfaces;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public abstract class TenantServiceRegistrar<TSelf, TServiceProvider, TSingletonScope> : ServiceRegistrar<TServiceProvider>
    where TSelf : TenantServiceRegistrar<TSelf, TServiceProvider, TSingletonScope>
    where TServiceProvider : IServiceProvider 
    where TSingletonScope : IServiceScope
{
    public new OnBeforeTenantBuild<TSelf, TServiceProvider> OnBeforeBuild { get; set; } = _ => { };
    public override ValueTask<TServiceProvider> BuildAsync()
    {
        throw new NotImplementedException();
    }

    public async ValueTask<IServiceProvider> BuildAsync(IServiceProvider rootServiceProvider)
    {
        OnBeforeBuild((TSelf)this);

        var serviceProviderRoot = (ServiceProviderRoot<TSingletonScope>)rootServiceProvider;
        
        var serviceProvider = new TenantServiceProvider<TSingletonScope>(serviceProviderRoot, ServiceFactoryBuilders.AsReadOnly());
        
        var vt = serviceProvider.InitializeAsync();

        if (!vt.IsCompletedSuccessfully)
        {
            await vt.ConfigureAwait(false);
        }
        
        return serviceProvider;
    }
}