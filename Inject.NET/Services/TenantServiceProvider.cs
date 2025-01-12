using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public class TenantServiceProvider : IServiceProvider
{
    public readonly TenantedSingletonScope SingletonScope;
    public readonly ServiceProviderRoot RootServiceProviderRoot;
    
    internal readonly ServiceFactories ServiceFactories;

    public TenantServiceProvider(ServiceProviderRoot rootServiceProviderRoot, ServiceFactories serviceFactories)
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
        return new TenantedScope(this, (ServiceScope)RootServiceProviderRoot.CreateScope(), SingletonScope, ServiceFactories);
    }
}