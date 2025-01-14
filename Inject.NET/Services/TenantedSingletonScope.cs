﻿using Inject.NET.Extensions;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using Inject.NET.Pools;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public class TenantedSingletonScope<TSelf, TServiceProviderRoot, TDefaultSingletonScope, TDefaultScope>(
    TenantServiceProvider<TSelf, TServiceProviderRoot, TDefaultSingletonScope, TDefaultScope> tenantServiceProvider,
    TServiceProviderRoot root,
    ServiceFactories serviceFactories) : IServiceScope where TDefaultSingletonScope : SingletonScope where TServiceProviderRoot : ServiceProviderRoot<TServiceProviderRoot, TDefaultSingletonScope> 
    where TSelf : TenantedSingletonScope<TSelf, TServiceProviderRoot, TDefaultSingletonScope, TDefaultScope>
    where TDefaultScope : ServiceScope<TServiceProviderRoot, TDefaultSingletonScope>
{
    public TServiceProviderRoot Root { get; } = root;
    private static readonly Type ServiceScopeType = typeof(IServiceScope);
    private static readonly Type ServiceProviderType = typeof(IServiceProvider);
    
    private Dictionary<ServiceKey, object>? _registered;
    private Dictionary<ServiceKey, List<object>>? _registeredEnumerables;
    
    private readonly SingletonScope _scope = new(root, serviceFactories);

    public T Register<T>(ServiceKey key, T value)
    {
        (_registered ??= DictionaryPool<ServiceKey, object>.Shared.Get()).Add(key, value!);
        
        (_registeredEnumerables ??= DictionaryPool<ServiceKey, List<object>>.Shared.Get())
            .GetOrAdd(key, _ => [])
            .Add(value!);

        return value;
    }
    
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
        
        return _scope.GetService(serviceKey) ?? Root.SingletonScope.GetService(serviceKey);
    }

    public IReadOnlyList<object> GetServices(ServiceKey serviceKey)
    {
        if (Root.TryGetSingletons(serviceKey, out var defaultSingletons))
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
        foreach (var item in _registeredEnumerables ?? [])
        {
            foreach (var obj in item.Value)
            {
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                else if (obj is IAsyncDisposable asyncDisposable)
                {
                    _ = asyncDisposable.DisposeAsync();
                }
            }
        }
        
        DictionaryPool<ServiceKey, object>.Shared.Return(_registered);
        DictionaryPool<ServiceKey, List<object>>.Shared.Return(_registeredEnumerables);
            
        _scope.Dispose();
    }
    
    public void PreBuild() => _scope.PreBuild();
    public Task FinalizeAsync() => _scope.FinalizeAsync();
}