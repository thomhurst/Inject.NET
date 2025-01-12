using Inject.NET.Extensions;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public class TenantedScope<TSingletonScope>(
    TenantServiceProvider<TSingletonScope> tenantServiceProvider,
    ServiceScope<TSingletonScope> defaultScope,
    TenantedSingletonScope<TSingletonScope> singletonScope,
    ServiceFactories serviceFactories) : IServiceScope
    where TSingletonScope : IServiceScope
{
    public ServiceScope<TSingletonScope> DefaultScope { get; } = defaultScope;
    private static readonly Type ServiceScopeType = typeof(IServiceScope);
    private static readonly Type ServiceProviderType = typeof(IServiceProvider);
    
    private readonly ServiceScope<TSingletonScope> _scope = new((ServiceProviderRoot<TSingletonScope>)defaultScope.ServiceProvider, singletonScope, serviceFactories);

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