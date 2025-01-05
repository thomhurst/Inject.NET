using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal class TenantServiceProvider(ServiceProviderRoot rootServiceProviderRoot, ServiceFactories serviceFactories)
    : IServiceProvider
{
    private readonly TenantedSingletonScope _singletonScope = new(rootServiceProviderRoot, serviceFactories);
    
    internal async ValueTask InitializeAsync()
    {
        _singletonScope.PreBuild();

        await using var scope = CreateScope();
        
        foreach (var type in serviceFactories.Descriptors.Keys)
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
        return new TenantedScope(rootServiceProviderRoot.CreateScope(), _singletonScope, serviceFactories);
    }
}