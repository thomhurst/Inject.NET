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
        
        foreach (var type in serviceFactories.EnumerableDescriptors.Keys)
        {
            scope.GetServices(type);
        }
        
        foreach (var (type, keyedFactory) in serviceFactories.KeyedEnumerableDescriptors)
        {
            foreach (var key in keyedFactory.Keys)
            {
                scope.GetServices(type, key);
            }
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