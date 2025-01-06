using Inject.NET.Extensions;
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
        if (type.IsIEnumerable())
        {
            return GetServices(type);
        }
        
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
        
        return _scope.GetService(serviceKey) ?? defaultScope.GetService(serviceKey, this) ?? defaultScope.GetService(serviceKey);
    }

    public IEnumerable<object> GetServices(ServiceKey serviceKey)
    {
        IEnumerable<object> services = [
            ..defaultScope.GetServices(serviceKey),
            ..defaultScope.GetServices(serviceKey, this),
            .._scope.GetServices(serviceKey)
        ];
        
        return services.Distinct();
    }

    public IServiceProvider ServiceProvider => tenantServiceProvider;

    public async ValueTask DisposeAsync()
    {
        await defaultScope.DisposeAsync();
        await _scope.DisposeAsync();
    }
}