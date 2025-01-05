using Inject.NET.Enums;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using Inject.NET.Pools;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal sealed class ServiceScope(ServiceProviderRoot root, IServiceScope singletonScope, ServiceFactories serviceFactories)
    : IServiceScope
{
    private Dictionary<CacheKey, object>? _cachedObjects;
    private Dictionary<CacheKey, List<object>>? _cachedEnumerables;
    
    private List<object>? _forDisposal;
    
    public IServiceProvider Root { get; } = root;
    
    public object? GetService(Type type)
    {
        return GetService(new CacheKey(type));
    }

    public IEnumerable<object> GetServices(Type type)
    {
        return GetServices(new CacheKey(type));
    }

    public object? GetService(Type type, string? key)
    {
        return GetService(new CacheKey(type, key));
    }

    private object? GetService(CacheKey cacheKey)
    {
        if (_cachedObjects?.TryGetValue(cacheKey, out var cachedObject) == true)
        {
            return cachedObject;
        }
        
        if (serviceFactories.Descriptor.TryGetValue(cacheKey, out var descriptor))
        {
            if (descriptor.Lifetime == Lifetime.Singleton)
            {
                return singletonScope.GetService(cacheKey.Type, cacheKey.Key);
            }
            
            var obj = Constructor.Construct(this, cacheKey.Type, descriptor);
            
            if(descriptor.Lifetime != Lifetime.Transient)
            {
                (_cachedObjects ??= DictionaryPool<CacheKey, object>.Shared.Get())[cacheKey] = obj;
            }

            (_forDisposal ??= ListPool<object>.Shared.Get()).Add(obj);

            return obj;
        }
        
        return GetServices(cacheKey).LastOrDefault();
    }

    public IEnumerable<object> GetServices(Type type, string? key)
    {
        return GetServices(new CacheKey(type, key));
    }

    private IEnumerable<object> GetServices(CacheKey cacheKey)
    {
        var cachedEnumerables = _cachedEnumerables ??= DictionaryPool<CacheKey, List<object>>.Shared.Get();
        
        if (cachedEnumerables.TryGetValue(cacheKey, out var cachedObjects))
        {
            foreach (var cachedObject in cachedObjects)
            {
                yield return cachedObject;
            }
            
            yield break;
        }

        if (!serviceFactories.Descriptors.TryGetValue(cacheKey, out var factories))
        {
            yield break;
        }
        
        if (!root.TryGetSingletons(cacheKey.Type, cacheKey.Key, out var singletons))
        {
            singletons = [];
        }

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
                item = Constructor.Construct(this, cacheKey.Type, serviceDescriptor);
                
                (_forDisposal ??= ListPool<object>.Shared.Get()).Add(item);
            }
            
            if(lifetime != Lifetime.Transient)
            {
                if (!cachedEnumerables.TryGetValue(cacheKey, out var items))
                {
                    items = [];
                }
                
                items.Add(item);
            }

            yield return item;
        }
    }
    
    public ValueTask DisposeAsync()
    {
        DictionaryPool<CacheKey, object>.Shared.Return(_cachedObjects);
        DictionaryPool<CacheKey, List<object>>.Shared.Return(_cachedEnumerables);
        
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
}