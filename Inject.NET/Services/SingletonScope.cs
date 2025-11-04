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

    protected readonly List<object?> ConstructedObjects = [];

    // Cache for singleton collections created via dictionary-based resolution
    private readonly ConcurrentDictionary<ServiceKey, IReadOnlyList<object>> _singletonCollectionCache = new();

    public TServiceProvider ServiceProvider { get; } = serviceProvider;
    
    public T Register<T>(T value)
    {
        ConstructedObjects.Add(value);

        return value;
    }

    public async Task InitializeAsync()
    {
        foreach (var singletonAsyncInitialization in ConstructedObjects
                     .OfType<ISingletonAsyncInitialization>()
                     .OrderBy(x => x.Order))
        {
            await singletonAsyncInitialization.InitializeAsync();
        }
    }

    public object? GetService(Type type)
    {
        var serviceKey = new ServiceKey(type);

        if (type.IsIEnumerable())
        {
            // Extract element type from IEnumerable<T> and get all services of that type
            var elementType = type.GetGenericArguments()[0];
            return GetServices(serviceKey with { Type = elementType });
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

    public virtual object? GetService(ServiceKey serviceKey, IServiceScope originatingScope)
    {
        if (serviceKey.Type == Types.ServiceScope)
        {
            return this;
        }
        
        if (serviceKey.Type == Types.ServiceProvider || serviceKey.Type == Types.SystemServiceProvider)
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

    public virtual IReadOnlyList<object> GetServices(ServiceKey serviceKey, IServiceScope originatingScope)
    {
        // Check cache first - singletons should only be created once
        if (_singletonCollectionCache.TryGetValue(serviceKey, out var cached))
        {
            return cached;
        }

        // Lookup in service factories dictionary and filter for singletons
        if (!serviceFactories.Descriptors.TryGetValue(serviceKey, out var descriptors))
        {
            return [];
        }

        var singletonDescriptors = descriptors.Items.Where(d => d.Lifetime == Lifetime.Singleton).ToList();

        if (singletonDescriptors.Count == 0)
        {
            return [];
        }

        // Create instances (only happens once per serviceKey)
        var results = new List<object>(singletonDescriptors.Count);

        foreach (var descriptor in singletonDescriptors)
        {
            var obj = descriptor.Factory(originatingScope, serviceKey.Type, descriptor.Key);
            Register(obj); // Add to ConstructedObjects for disposal
            results.Add(obj);
        }

        // Cache the results so subsequent calls return the same instances
        var resultList = results.AsReadOnly();
        _singletonCollectionCache[serviceKey] = resultList;

        return resultList;
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
        foreach (var item in ConstructedObjects)
        {
            await Disposer.DisposeAsync(item);
        }
    }

    public void Dispose()
    {
        foreach (var item in ConstructedObjects)
        {
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
            else if (item is IAsyncDisposable asyncDisposable)
            {
                _ = asyncDisposable.DisposeAsync();
            }
        }
    }
}