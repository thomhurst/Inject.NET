using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal class TenantServiceProvider(ServiceProvider defaultServiceProvider, ServiceFactories serviceFactories)
    : IServiceProvider
{
    private readonly TenantedSingletonScope _singletonScope = new(defaultServiceProvider, serviceFactories);

    internal ValueTask InitializeAsync()
    {
        return _singletonScope.BuildAsync();
    }
    
    public async ValueTask DisposeAsync()
    {
        await _singletonScope.DisposeAsync();
    }

    public IServiceScope CreateScope()
    {
        return new TenantedScope(defaultServiceProvider.CreateScope(), serviceFactories);
    }
}