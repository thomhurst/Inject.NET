using Inject.NET.Extensions;
using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

public class TenantedScope<TServiceProvider, TSingletonScope, TDefaultSingletonScope, TDefaultScope>(
    TServiceProvider serviceProviderRoot,
    TDefaultScope defaultScope,
    TDefaultSingletonScope defaultSingletonScope,
    TSingletonScope singletonScope,
    ServiceFactories serviceFactories) : IServiceScope, IScoped
    where TSingletonScope : TenantedSingletonScope<TSingletonScope, TServiceProvider, TDefaultSingletonScope, TDefaultScope> 
    where TServiceProvider : ServiceProviderRoot<TServiceProvider, TDefaultSingletonScope, TDefaultScope>
    where TDefaultScope : ServiceScope<TServiceProvider, TDefaultSingletonScope, TDefaultScope>
    where TDefaultSingletonScope : SingletonScope<TDefaultSingletonScope, TServiceProvider, TDefaultScope>
{
    public TDefaultScope DefaultScope { get; } = defaultScope;
    private static readonly Type ServiceScopeType = typeof(IServiceScope);
    private static readonly Type ServiceProviderType = typeof(IServiceProvider);
    
    private readonly ServiceScope<TServiceProvider, TDefaultSingletonScope, TDefaultScope> _scope = new(serviceProviderRoot, defaultSingletonScope, serviceFactories);

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

    public TServiceProvider ServiceProvider => serviceProviderRoot;

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