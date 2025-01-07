using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal class TenantServiceProvider : IServiceProvider
{
    private readonly TenantedSingletonScope _singletonScope;
    private readonly ServiceProviderRoot _rootServiceProviderRoot;
    
    internal readonly ServiceFactories ServiceFactories;

    public TenantServiceProvider(ServiceProviderRoot rootServiceProviderRoot, ServiceFactories serviceFactories)
    {
        _rootServiceProviderRoot = rootServiceProviderRoot;
        ServiceFactories = serviceFactories;
        _singletonScope = new(this, rootServiceProviderRoot, serviceFactories);
    }

    internal async ValueTask InitializeAsync()
    {
        _singletonScope.PreBuild();

        await using var scope = CreateScope();
        
        foreach (var type in ServiceFactories.Descriptors.Keys)
        {
            scope.GetService(type);
        }
        
        await _singletonScope.FinalizeAsync();
    }
    
    public async ValueTask DisposeAsync()
    {
        await _singletonScope.DisposeAsync();
    }

    public IServiceScope CreateScope()
    {
        return new TenantedScope(this, (ServiceScope)_rootServiceProviderRoot.CreateScope(), _singletonScope, ServiceFactories);
    }
}