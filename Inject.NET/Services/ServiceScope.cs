using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using Inject.NET.Enums;
using Inject.NET.Extensions;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using Inject.NET.Pools;

namespace Inject.NET.Services;

public class ServiceScope<TSelf, TServiceProvider, TSingletonScope, TParentScope, TParentSingletonScope, TParentServiceProvider> : IServiceScope<TSelf, TServiceProvider, TSingletonScope>, IScoped
    where TServiceProvider : ServiceProvider<TServiceProvider, TSingletonScope, TSelf, TParentServiceProvider, TParentSingletonScope, TParentScope>, IServiceProvider<TSelf>
    where TSingletonScope : SingletonScope<TSingletonScope, TServiceProvider, TSelf, TParentSingletonScope, TParentScope, TParentServiceProvider>
    where TSelf : ServiceScope<TSelf, TServiceProvider, TSingletonScope, TParentScope, TParentSingletonScope, TParentServiceProvider>
    where TParentScope : IServiceScope
    where TParentSingletonScope : IServiceScope
{
    private static readonly Type ServiceScopeType = typeof(IServiceScope);
    private static readonly Type ServiceProviderType = typeof(IServiceProvider);
    
    public IServiceScope scope { get; }
    
#if NET9_0_OR_GREATER
    private bool _disposed;
#else
    private int _disposed;
#endif
    
    private Dictionary<ServiceKey, object>? _registered;
    private Dictionary<ServiceKey, List<object>>? _registeredEnumerables;
    
    private Dictionary<ServiceKey, object>? _cachedObjects;
    private Dictionary<ServiceKey, List<object>>? _cachedEnumerables;
    
    private List<object>? _forDisposal;
    private readonly TServiceProvider _root;
    private readonly ServiceFactories _serviceFactories;
    private readonly TParentScope? _parentScope;

    public ServiceScope(TServiceProvider serviceProvider, TSingletonScope singletonScope, ServiceFactories serviceFactories, TParentScope? parentScope)
    {
        _root = serviceProvider;
        _serviceFactories = serviceFactories;
        _parentScope = parentScope;
        SingletonScope = singletonScope;
        ServiceProvider = serviceProvider;
        scope = this;
    }

    public TSingletonScope SingletonScope { get; }

    public TServiceProvider ServiceProvider { get; }

    public T Register<T>(ServiceKey key, T obj)
    {
        (_registered ??= DictionaryPool<ServiceKey, object>.Shared.Get()).Add(key, obj!);
        
        (_registeredEnumerables ??= DictionaryPool<ServiceKey, List<object>>.Shared.Get())
            .GetOrAdd(key, _ => [])
            .Add(obj!);

        return obj;
    }
    
    public object? GetService(Type type)
    {
        var serviceKey = new ServiceKey(type);
        
        if (type.IsIEnumerable())
        {
            if (GetServices(serviceKey, this) is { Count: > 0 } services)
            {
                return services;
            }
            
            return _parentScope?.GetServices(serviceKey) ?? Array.Empty<object>();
        }
        
        return GetService(serviceKey);
    }

    public object? GetService(ServiceKey serviceKey)
    {
        if (GetService(serviceKey, this) is { } service)
        {
            return service;
        }

        return _parentScope?.GetService(serviceKey);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public object? GetService(ServiceKey serviceKey, IServiceScope requestingScope)
    {
        if (_registered?.TryGetValue(serviceKey, out var value) == true)
        {
            return value;
        }
        
        if (_cachedObjects?.TryGetValue(serviceKey, out var cachedObject) == true)
        {
            return cachedObject;
        }
        
        if (serviceKey.Type == ServiceScopeType)
        {
            return this;
        }
        
        if (serviceKey.Type == ServiceProviderType)
        {
            return ServiceProvider;
        }

        if (_cachedEnumerables?.TryGetValue(serviceKey, out var cachedEnumerable) == true
            && cachedEnumerable.Count > 0)
        {
            return cachedEnumerable[^1];
        }

        if (!_serviceFactories.Descriptor.TryGetValue(serviceKey, out var descriptor))
        {
            if (!_serviceFactories.LateBoundGenericDescriptor.TryGetValue(serviceKey, out descriptor))
            {
                if (!serviceKey.Type.IsGenericType ||
                    !_serviceFactories.Descriptor.TryGetValue(
                        serviceKey with { Type = serviceKey.Type.GetGenericTypeDefinition() }, out descriptor))
                {
                    return null;
                }

                _serviceFactories.LateBoundGenericDescriptor[serviceKey] = descriptor;
            }
        }

        if (descriptor.Lifetime == Lifetime.Singleton)
        {
            return SingletonScope.GetService(serviceKey);
        }

        var obj = descriptor.Factory(requestingScope, serviceKey.Type, descriptor.Key);
            
        if(descriptor.Lifetime != Lifetime.Transient)
        {
            (_cachedObjects ??= DictionaryPool<ServiceKey, object>.Shared.Get())[serviceKey] = obj;
        }

        if(obj is IAsyncDisposable or IDisposable)
        {
            (_forDisposal ??= ListPool<object>.Shared.Get()).Add(obj);
        }

        return obj;
    }

    public IReadOnlyList<object> GetServices(ServiceKey serviceKey)
    {
        return GetServices(serviceKey, this);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public IReadOnlyList<object> GetServices(ServiceKey serviceKey, IServiceScope scope)
    {
        if (_cachedEnumerables?.TryGetValue(serviceKey, out var cachedObjects) == true)
        {
            return cachedObjects;
        }

        if (!_serviceFactories.Descriptors.TryGetValue(serviceKey, out var factories))
        {
            return Array.Empty<object>();
        }
        
        if (!_root.TryGetSingletons(serviceKey, out var singletons))
        {
            singletons = Array.Empty<object>();
        }

        var cachedEnumerables = _cachedEnumerables ??= DictionaryPool<ServiceKey, List<object>>.Shared.Get();

        return cachedEnumerables[serviceKey] = [..ConstructItems(factories, singletons, scope, serviceKey, cachedEnumerables)];
    }

    private IEnumerable<object> ConstructItems(FrozenSet<ServiceDescriptor> factories,
        IReadOnlyList<object> singletons,
        IServiceScope scope, ServiceKey serviceKey, Dictionary<ServiceKey, List<object>> cachedEnumerables)
    {
        var singletonIndex = 0;
        for (var i = 0; i < factories.Count; i++)
        {
            var serviceDescriptor = factories.Items[i];
            var lifetime = serviceDescriptor.Lifetime;
            
            object? item;
            if (lifetime == Lifetime.Singleton)
            {
                item = singletons[singletonIndex++];
            }
            else
            {
                item = serviceDescriptor.Factory(scope, serviceKey.Type, serviceDescriptor.Key);
                
                if(item is IAsyncDisposable or IDisposable)
                {
                    (_forDisposal ??= ListPool<object>.Shared.Get()).Add(item);
                }
            }
            
            if(lifetime != Lifetime.Transient)
            {
                if (!cachedEnumerables.TryGetValue(serviceKey, out var items))
                {
                    cachedEnumerables[serviceKey] = items = [];
                }
                
                items.Add(item);
            }

            yield return item;
        }
    }

    public ValueTask DisposeAsync()
    {
#if NET9_0_OR_GREATER
        if (Interlocked.Exchange(ref _disposed, true))
        {
            return default;
        }
#else
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return default;
        }
#endif

        DictionaryPool<ServiceKey, object>.Shared.Return(_cachedObjects);
        DictionaryPool<ServiceKey, List<object>>.Shared.Return(_cachedEnumerables);
        
        if (Interlocked.Exchange(ref _forDisposal, null) is not {} forDisposal)
        {
            return default;
        }
        
        for (var i = forDisposal.Count - 1; i >= 0; i--)
        {
            var obj = forDisposal[i];
       
            if (obj is IAsyncDisposable asyncDisposable)
            {
                var vt = asyncDisposable.DisposeAsync();
                
                if (!vt.IsCompleted)
                {
                    return Await(--i, vt, forDisposal);
                }
            }
            else if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        
        ListPool<object>.Shared.Return(forDisposal);
        
        return default;
        
        static async ValueTask Await(int i, ValueTask vt, List<object> toDispose)
        {
            await vt.ConfigureAwait(false);

            for (; i >= 0; i--)
            {
                var disposable = toDispose[i];
                if (disposable is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else
                {
                    ((IDisposable)disposable).Dispose();
                }
            }
            
            ListPool<object>.Shared.Return(toDispose); 
        }
    }

    public void Dispose()
    {
        DictionaryPool<ServiceKey, object>.Shared.Return(_cachedObjects);
        DictionaryPool<ServiceKey, List<object>>.Shared.Return(_cachedEnumerables);
        
        if (Interlocked.Exchange(ref _forDisposal, null) is not {} forDisposal)
        {
            return;
        }
        
        for (var i = forDisposal.Count - 1; i >= 0; i--)
        {
            var toDispose = forDisposal[i];

            if (toDispose is IDisposable disposable)
            {
                disposable.Dispose();
            }
            else if(toDispose is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
        }
        
        ListPool<object>.Shared.Return(forDisposal);
    }
}