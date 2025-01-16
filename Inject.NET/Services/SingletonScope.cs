using System.Collections.Concurrent;
using Inject.NET.Enums;
using Inject.NET.Extensions;
using Inject.NET.Helpers;
using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

public class SingletonScope<TSelf, TServiceProvider, TScope, TParentSingletonScope, TParentServiceScope, TParentServiceProvider>(TServiceProvider serviceProvider, ServiceFactories serviceFactories, TParentSingletonScope? parentScope) : IServiceScope, ISingleton
where TServiceProvider : ServiceProvider<TServiceProvider, TSelf, TScope, TParentServiceProvider, TParentSingletonScope, TParentServiceScope>
where TSelf : SingletonScope<TSelf, TServiceProvider, TScope, TParentSingletonScope, TParentServiceScope, TParentServiceProvider>
where TScope : ServiceScope<TScope, TServiceProvider, TSelf, TParentServiceScope, TParentSingletonScope, TParentServiceProvider>
where TParentSingletonScope : IServiceScope
where TParentServiceScope : IServiceScope
{
    public TSelf Singletons => (TSelf)this;
    public TParentSingletonScope? ParentScope { get; } = parentScope;
    
    private Dictionary<ServiceKey, object>? _cachedObjects;
    private Dictionary<ServiceKey, List<object>>? _cachedEnumerables;

    private readonly ConcurrentDictionary<ServiceKey, List<object>> _singletonsBuilder = [];
    
    private Dictionary<ServiceKey, Func<object>>? _registeredFactories;
    private Dictionary<ServiceKey, List<Func<object>>>? _registeredEnumerableFactories;
    
    private bool _isBuilt;
    
    public TServiceProvider ServiceProvider { get; } = serviceProvider;

    public void PreBuild()
    {
        foreach (var cacheKey in GetSingletonKeys())
        {
            GetServices(cacheKey);
        }
    }
    
    public T Register<T>(ServiceKey key, T value)
    {
        (_cachedObjects ??= Pools.Objects.Get())[key] = value!;
        
        (_cachedEnumerables ??= Pools.Enumerables.Get())
            .GetOrAdd(key, _ => [])
            .Add(value!);

        return value;
    }
    
    public void Register(ServiceKey key, Func<object> value)
    {
        (_registeredFactories ??= Pools.Funcs.Get())[key] = value;
        
        (_registeredEnumerableFactories ??= Pools.EnumerableFuncs.Get())
            .GetOrAdd(key, _ => [])
            .Add(value);
    }

    internal async Task FinalizeAsync()
    {
        if(_cachedEnumerables is not null)
        {
            foreach (var singletonAsyncInitialization in _cachedEnumerables.SelectMany(s => s.Value)
                         .OfType<ISingletonAsyncInitialization>()
                         .OrderBy(x => x.Order))
            {
                await singletonAsyncInitialization.InitializeAsync();
            }
        }

        _isBuilt = true;
    }

    public object? GetService(Type type)
    {
        var serviceKey = new ServiceKey(type);
        
        if (type.IsIEnumerable())
        {
            return GetServices(serviceKey);
        }

        return GetService(serviceKey);
    }

    public object? GetService(ServiceKey serviceKey)
    {
        return GetService(serviceKey, this);
    }

    public IReadOnlyList<object> GetServices(ServiceKey serviceKey)
    {
        return GetServices(serviceKey, this);
    }

    public object? GetService(ServiceKey serviceKey, IServiceScope originatingScope)
    {
        if (_cachedObjects?.TryGetValue(serviceKey, out var singleton) == true)
        {
            return singleton;
        }

        if (_registeredFactories?.TryGetValue(serviceKey, out var factory) == true)
        {
            return (_cachedObjects ??= Pools.Objects.Get())[serviceKey] = factory();
        }
        
        if (serviceKey.Type == Types.ServiceScope)
        {
            return this;
        }
        
        if (serviceKey.Type == Types.ServiceProvider)
        {
            return ServiceProvider;
        }

        var services = GetServices(serviceKey);

        if (services.Count == 0)
        {
            return ParentScope?.GetService(serviceKey, originatingScope);
        }
        
        return services[^1];
    }

    public IReadOnlyList<object> GetServices(ServiceKey serviceKey, IServiceScope originatingScope)
    {
        if (_cachedEnumerables?.TryGetValue(serviceKey, out var singletons) == true)
        {
            return _cachedEnumerables[serviceKey] = singletons;
        }
        
        if (_registeredEnumerableFactories?.TryGetValue(serviceKey, out var factory) == true)
        {
            return (_cachedEnumerables ??= Pools.Enumerables.Get())[serviceKey] = factory.Select(x => x()).ToList();
        }

        if (!_isBuilt)
        {
            if (_singletonsBuilder.TryGetValue(serviceKey, out var cache))
            {
                return cache;
            }

            if (!serviceFactories.Descriptors.TryGetValue(serviceKey, out var descriptors))
            {
                return ParentScope?.GetServices(serviceKey, originatingScope) ?? Array.Empty<object>();
            }

            return _singletonsBuilder[serviceKey] =
            [
                ..descriptors
                    .Where(x => x.Lifetime == Lifetime.Singleton)
                    .Select(descriptor => descriptor.Factory(originatingScope, serviceKey.Type, descriptor.Key))
            ];
        }

        return [];
    }

    private IEnumerable<ServiceKey> GetSingletonKeys()
    {
        return serviceFactories.Descriptors
            .Where(x => x.Value.Items.Any(y => y.Lifetime == Lifetime.Singleton))
            .GroupBy(x => x.Key)
            .Select(x => x.Key)
            .Where(x => !x.Type.IsGenericTypeDefinition);
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var item in _cachedEnumerables ?? Enumerable.Empty<KeyValuePair<ServiceKey, List<object>>>())
        {
            foreach (var obj in item.Value)
            {
                await Disposer.DisposeAsync(obj);
            }
        }
    }

    public void Dispose()
    {
        foreach (var item in _cachedEnumerables ?? Enumerable.Empty<KeyValuePair<ServiceKey, List<object>>>())
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
    }
}