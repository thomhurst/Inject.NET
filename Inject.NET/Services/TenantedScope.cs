using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal sealed class TenantedScope(
    TenantServiceProvider tenantServiceProvider,
    IServiceScope defaultScope,
    IServiceScope singletonScope,
    ServiceFactories serviceFactories) : IServiceScope
{
    private static readonly Type ServiceScopeType = typeof(IServiceScope);
    private static readonly Type ServiceProviderType = typeof(IServiceProvider);
    
    private readonly ServiceScope _scope = new((ServiceProviderRoot)defaultScope.ServiceProvider, singletonScope, serviceFactories);

    public object? GetService(Type type)
    {
        if (type == ServiceScopeType)
        {
            return this;
        }
        
        if (type == ServiceProviderType)
        {
            return ServiceProvider;
        }
        
        return _scope.GetService(type);
    }

    public IEnumerable<object> GetServices(Type type)
    {
        return
        [
            defaultScope.GetServices(type),
            .._scope.GetServices(type)
        ];
    }

    public object? GetService(Type type, string? key)
    {
        return _scope.GetService(type, key);
    }

    public IEnumerable<object> GetServices(Type type, string? key)
    {
        return
        [
            defaultScope.GetServices(type, key),
            .._scope.GetServices(type, key)
        ];
    }

    public IServiceProvider ServiceProvider => tenantServiceProvider;

    public async ValueTask DisposeAsync()
    {
        await defaultScope.DisposeAsync();
        await _scope.DisposeAsync();
    }
}