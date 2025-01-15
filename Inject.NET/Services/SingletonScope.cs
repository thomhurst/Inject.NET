using System.Collections.Concurrent;
using Inject.NET.Enums;
using Inject.NET.Extensions;
using Inject.NET.Helpers;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using Inject.NET.Pools;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public class SingletonScope<TSelf, TServiceProvider, TScope, TParentSingletonScope, TParentServiceScope, TParentServiceProvider>(TServiceProvider serviceProvider, ServiceFactories serviceFactories, TParentSingletonScope? parentScope) : IServiceScope, ISingleton
where TServiceProvider : ServiceProvider<TServiceProvider, TSelf, TScope, TParentServiceProvider, TParentSingletonScope, TParentServiceScope>
where TSelf : SingletonScope<TSelf, TServiceProvider, TScope, TParentSingletonScope, TParentServiceScope, TParentServiceProvider>
where TScope : ServiceScope<TScope, TServiceProvider, TSelf, TParentServiceScope, TParentSingletonScope, TParentServiceProvider>
where TParentSingletonScope : IServiceScope
where TParentServiceScope : IServiceScope
{
    private static readonly Type ServiceScopeType = typeof(IServiceScope);
    private static readonly Type ServiceProviderType = typeof(IServiceProvider);
    
    private Dictionary<ServiceKey, object>? _registered;
    private Dictionary<ServiceKey, List<object>>? _registeredEnumerables;

    private readonly ConcurrentDictionary<ServiceKey, List<object>> _singletonsBuilder = [];
    
    private ConcurrentDictionary<ServiceKey, IReadOnlyList<object>> _singletonEnumerables = [];
    
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
        (_registered ??= DictionaryPool<ServiceKey, object>.Shared.Get()).Add(key, value!);
        
        (_registeredEnumerables ??= DictionaryPool<ServiceKey, List<object>>.Shared.Get())
            .GetOrAdd(key, _ => [])
            .Add(value!);

        return value;
    }

    internal async Task FinalizeAsync()
    {
        if(_registeredEnumerables is not null)
        {
            foreach (var singletonAsyncInitialization in _registeredEnumerables.SelectMany(s => s.Value)
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
        if (_registered?.TryGetValue(serviceKey, out var singleton) == true)
        {
            return singleton;
        }
        
        if (serviceKey.Type == ServiceScopeType)
        {
            return this;
        }
        
        if (serviceKey.Type == ServiceProviderType)
        {
            return ServiceProvider;
        }

        var services = GetServices(serviceKey);

        if (services.Count == 0)
        {
            return parentScope?.GetService(serviceKey, originatingScope);
        }
        
        return services[^1];
    }

    public IReadOnlyList<object> GetServices(ServiceKey serviceKey, IServiceScope originatingScope)
    {
        if (_singletonEnumerables.TryGetValue(serviceKey, out var list))
        {
            return list;
        }
        
        if (_registeredEnumerables?.TryGetValue(serviceKey, out var singletons) == true)
        {
            return _singletonEnumerables[serviceKey] = singletons;
        }

        if (!_isBuilt)
        {
            if (_singletonsBuilder.TryGetValue(serviceKey, out var cache))
            {
                return cache;
            }

            if (!serviceFactories.Descriptors.TryGetValue(serviceKey, out var descriptors))
            {
                return parentScope?.GetServices(serviceKey, originatingScope) ?? Array.Empty<object>();
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
        foreach (var item in _singletonEnumerables)
        {
            foreach (var obj in item.Value)
            {
                await Disposer.DisposeAsync(obj);
            }
        }
    }

    public void Dispose()
    {
        foreach (var item in _singletonEnumerables)
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