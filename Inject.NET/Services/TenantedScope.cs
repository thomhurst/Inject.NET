using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal sealed class TenantedScope(
    TenantServiceProvider tenantServiceProvider,
    ServiceScope defaultScope,
    TenantedSingletonScope singletonScope,
    ServiceFactories serviceFactories) : IServiceScope
{
    private static readonly Type ServiceScopeType = typeof(IServiceScope);
    private static readonly Type ServiceProviderType = typeof(IServiceProvider);
    
    private readonly ServiceScope _scope = new((ServiceProviderRoot)defaultScope.ServiceProvider, singletonScope, serviceFactories);

    public object? GetService(Type type)
    {
        return GetService(new ServiceKey(type));
    }
    
    
    public object? GetService(ServiceKey serviceKey)
    {
        if (serviceKey.Type == ServiceScopeType)
        {
            return this;
        }
        
        if (serviceKey.Type == ServiceProviderType)
        {
            return ServiceProvider;
        }
        
        return _scope.GetService(serviceKey);
    }

    public IEnumerable<object> GetServices(ServiceKey serviceKey)
    {
        return
        [
            ..defaultScope.GetServices(serviceKey),
            .._scope.GetServices(serviceKey)
        ];
    }

    public IServiceProvider ServiceProvider => tenantServiceProvider;

    public async ValueTask DisposeAsync()
    {
        await defaultScope.DisposeAsync();
        await _scope.DisposeAsync();
    }
}