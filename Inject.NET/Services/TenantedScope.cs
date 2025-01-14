using Inject.NET.Extensions;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public class TenantedScope<TServiceProvider, TSingletonScope>(
    TenantServiceProvider<TServiceProvider, TSingletonScope> tenantServiceProvider,
    ServiceScope<TServiceProvider, TSingletonScope> defaultScope,
    TSingletonScope singletonScope,
    ServiceFactories serviceFactories) : IServiceScope
    where TSingletonScope : SingletonScope 
    where TServiceProvider : ServiceProviderRoot<TServiceProvider, TSingletonScope>
{
    public ServiceScope<TServiceProvider, TSingletonScope> DefaultScope { get; } = defaultScope;
    private static readonly Type ServiceScopeType = typeof(IServiceScope);
    private static readonly Type ServiceProviderType = typeof(IServiceProvider);
    
    private readonly ServiceScope<TServiceProvider, TSingletonScope> _scope = new(defaultScope.ServiceProvider, singletonScope, serviceFactories);

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
        
        return _scope.GetService(serviceKey) ?? DefaultScope.GetService(serviceKey, this) ?? DefaultScope.GetService(serviceKey);
    }

    public IReadOnlyList<object> GetServices(ServiceKey serviceKey)
    {
        return
        [
            ..DefaultScope.GetServices(serviceKey, this),
            .._scope.GetServices(serviceKey)
        ];
    }

    public IServiceScope SingletonScope { get; } = singletonScope;

    public IServiceProvider ServiceProvider => tenantServiceProvider;

    public async ValueTask DisposeAsync()
    {
        await DefaultScope.DisposeAsync();
        await _scope.DisposeAsync();
    }

    public void Dispose()
    {
        DefaultScope.Dispose();
        _scope.Dispose();
    }
}