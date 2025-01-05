using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal class TenantServiceProvider(ServiceProvider rootServiceProvider, ServiceFactories serviceFactories)
    : IServiceProvider
{
    private readonly TenantedSingletonScope _singletonScope = new(rootServiceProvider, serviceFactories);

    internal async ValueTask InitializeAsync()
    {
        _singletonScope.PreBuild();

        await using var scope = CreateScope();
        
        foreach (var type in serviceFactories.Factories.Keys)
        {
            scope.GetServices(type);
        }
        
        foreach (var (type, keyedFactory) in serviceFactories.KeyedFactories)
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
        return new TenantedScope(rootServiceProvider.CreateScope(), serviceFactories);
    }
}