using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal class TenantServiceProvider : IServiceProvider
{
    private readonly TenantedSingletonScope _singletonScope;
    private readonly ServiceProviderRoot _rootServiceProviderRoot;
    private readonly ServiceFactories _serviceFactories;

    public TenantServiceProvider(ServiceProviderRoot rootServiceProviderRoot, ServiceFactories serviceFactories)
    {
        _rootServiceProviderRoot = rootServiceProviderRoot;
        _serviceFactories = serviceFactories;
        _singletonScope = new(this, rootServiceProviderRoot, serviceFactories);
    }

    internal async ValueTask InitializeAsync()
    {
        _singletonScope.PreBuild();

        await using var scope = CreateScope();
        
        foreach (var type in _serviceFactories.Descriptors.Keys)
        {
            scope.GetServices(type.Type, type.Key);
        }
        
        await _singletonScope.FinalizeAsync();
    }
    
    public async ValueTask DisposeAsync()
    {
        await _singletonScope.DisposeAsync();
    }

    public IServiceScope CreateScope()
    {
        return new TenantedScope(this, _rootServiceProviderRoot.CreateScope(), _singletonScope, _serviceFactories);
    }
}