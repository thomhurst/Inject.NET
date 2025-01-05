using System.Collections.Concurrent;
using Inject.NET.Enums;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal class ServiceScope(ServiceProvider serviceProvider, ServiceFactories serviceFactories)
    : IServiceScope
{
    private readonly ConcurrentDictionary<Type, List<object>> _cachedObjects = [];
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, List<object>>> _cachedKeyedObjects = [];
    private readonly List<object> _forDisposal = [];
    
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    ~ServiceScope()
    {
        _ = DisposeAsync();
    }
    
    public object? GetService(Type type)
    {
        return GetServices(type).LastOrDefault();
    }

    public virtual IEnumerable<object> GetServices(Type type)
    {
        if (_cachedObjects.TryGetValue(type, out var cachedObjects))
        {
            foreach (var cachedObject in cachedObjects)
            {
                yield return cachedObject;
            }
            
            yield break;
        }

        if (!serviceFactories.Factories.TryGetValue(type, out var factories))
        {
            yield break;
        }
        
        if (!serviceProvider.TryGetSingletons(type, out var singletons))
        {
            singletons = [];
        }

        var singletonIndex = 0;
        for (var i = 0; i < factories.Count; i++)
        {
            var (lifetime, factory) = factories.Items[i];

            object item;
            if (lifetime == Lifetime.Singleton)
            {
                 item = singletons[singletonIndex++];
            }
            else
            {
                item = factory(this, type);
                _forDisposal.Add(item);
            }
            
            _cachedObjects.GetOrAdd(type, []).Add(item);

            yield return item;
        }
    }

    public object? GetService(Type type, string key)
    {
        return GetServices(type, key).LastOrDefault();
    }

    public virtual IEnumerable<object> GetServices(Type type, string key)
    {
        if (_cachedKeyedObjects.TryGetValue(type, out var dictionary)
            && dictionary.TryGetValue(key, out var cachedObjects))
        {
            foreach (var cachedObject in cachedObjects)
            {
                yield return cachedObject;
            }
            
            yield break;
        }

        if (!serviceFactories.KeyedFactories.TryGetValue(type, out var keyedFactories)
            || !keyedFactories.TryGetValue(key, out var factories))
        {
            yield break;
        }
        
        if (!serviceProvider.TryGetSingletons(type, key, out var singletons))
        {
            singletons = [];
        }

        var singletonIndex = 0;
        for (var i = 0; i < factories.Count; i++)
        {
            var (lifetime, factory) = factories.Items[i];

            object item;
            if (lifetime == Lifetime.Singleton)
            {
                item = singletons[singletonIndex++];
            }
            else
            {
                item = factory(this, type, key);
                _forDisposal.Add(item);
            }
            
            _cachedKeyedObjects.GetOrAdd(type, []).GetOrAdd(key, []).Add(item);

            yield return item;
        }
    }
    
    public virtual async ValueTask DisposeAsync()
    {
        await Parallel.ForEachAsync(_forDisposal, async (obj, _) =>
        {
            if (obj is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        });
    }
}