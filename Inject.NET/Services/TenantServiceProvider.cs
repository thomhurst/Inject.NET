using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public class TenantServiceProvider<TSingletonScope> : IServiceProvider
    where TSingletonScope : IServiceScope
{
    public readonly TenantedSingletonScope<TSingletonScope> SingletonScope;
    public readonly ServiceProviderRoot<TSingletonScope> RootServiceProviderRoot;
    
    internal readonly ServiceFactories ServiceFactories;

    public TenantServiceProvider(ServiceProviderRoot<TSingletonScope> rootServiceProviderRoot, ServiceFactories serviceFactories)
    {
        RootServiceProviderRoot = rootServiceProviderRoot;
        ServiceFactories = serviceFactories;
        SingletonScope = new(this, rootServiceProviderRoot, serviceFactories);
    }

    internal async ValueTask InitializeAsync()
    {
        SingletonScope.PreBuild();

        await using var scope = CreateScope();
        
        foreach (var type in ServiceFactories.Descriptors.Keys)
        {
            scope.GetService(type);
        }
        
        await SingletonScope.FinalizeAsync();
    }
    
    public async ValueTask DisposeAsync()
    {
        await SingletonScope.DisposeAsync();
    }

    public virtual IServiceScope CreateScope()
    {
        return new TenantedScope<TSingletonScope>(this, (ServiceScope<TSingletonScope>)RootServiceProviderRoot.CreateScope(), SingletonScope, ServiceFactories);
    }
}