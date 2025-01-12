using Inject.NET.Delegates;
using Inject.NET.Extensions;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public class TenantServiceRegistrar<TSingletonScope> : IServiceRegistrar
    where TSingletonScope : IServiceScope
{
    public ServiceFactoryBuilders ServiceFactoryBuilders { get; } = new();
    
    public IServiceRegistrar Register(ServiceDescriptor serviceDescriptor)
    {
        ServiceFactoryBuilders.Add(serviceDescriptor);

        return this;
    }

    public OnBeforeBuild OnBeforeBuild { get; set; } = _ => { };
    
    public async ValueTask<IServiceProvider> BuildAsync(IServiceProvider rootServiceProvider)
    {
        OnBeforeBuild(this);

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