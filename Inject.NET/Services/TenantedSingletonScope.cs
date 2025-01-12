﻿using Inject.NET.Extensions;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public class TenantedSingletonScope(
    TenantServiceProvider tenantServiceProvider,
    ServiceProviderRoot root,
    ServiceFactories serviceFactories) : IServiceScope
{
    private static readonly Type ServiceScopeType = typeof(IServiceScope);
    private static readonly Type ServiceProviderType = typeof(IServiceProvider);
    
    private readonly SingletonScope _scope = new(root, serviceFactories);

    public object? GetService(Type serviceType)
    {
        if (serviceType.IsIEnumerable())
        {
            return GetServices(serviceType);
        }
        
        return GetService(new ServiceKey(serviceType));
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
        
        return _scope.GetService(serviceKey) ?? root.SingletonScope.GetService(serviceKey);
    }

    public IReadOnlyList<object> GetServices(ServiceKey serviceKey)
    {
        if (root.TryGetSingletons(serviceKey, out var defaultSingletons))
        {
            return
            [
                ..defaultSingletons,
                .._scope.GetServices(serviceKey)
            ];
        }
        
        return _scope.GetServices(serviceKey);
    }

    public IServiceScope SingletonScope => this;

    public IServiceProvider ServiceProvider => tenantServiceProvider;

    public ValueTask DisposeAsync()
    {
        return _scope.DisposeAsync();
    }
    
    public void Dispose()
    {
        _scope.Dispose();
    }
    
    public void PreBuild() => _scope.PreBuild();
    public Task FinalizeAsync() => _scope.FinalizeAsync();
}