using System.Collections.Concurrent;
using System.Collections.Frozen;
using Inject.NET.Enums;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal class ServiceScope(ServiceProviderRoot root, SingletonScope singletonScope, ServiceFactories serviceFactories)
    : IServiceScope
{
    private ConcurrentDictionary<Type, object>? _cachedObjects;
    private ConcurrentDictionary<Type, ConcurrentDictionary<string, object>>? _cachedKeyedObjects;
    
    private ConcurrentDictionary<Type, List<object>>? _cachedEnumerables;
    private ConcurrentDictionary<Type, ConcurrentDictionary<string, List<object>>>? _cachedKeyedEnumerables;
    
    private List<object>? _forDisposal;
    
    public IServiceProvider Root { get; } = root;
    
    public object? GetService(Type type)
    {
        if (_cachedObjects?.TryGetValue(type, out var cachedObject) == true)
        {
            return cachedObject;
        }
        
        if (serviceFactories.Descriptor.TryGetValue(type, out var descriptor))
        {
            if (descriptor.Lifetime == Lifetime.Singleton)
            {
                return singletonScope.GetService(type);
            }
            
            var obj = Constructor.Construct(this, type, null, descriptor);
            
            (_cachedObjects ??= [])[type] = obj;
            (_forDisposal ??= []).Add(obj);

            return obj;
        }
        
        return GetServices(type).LastOrDefault();
    }

    public virtual IEnumerable<object> GetServices(Type type)
    {
        var cachedEnumerables = _cachedEnumerables ??= [];
        
        if (cachedEnumerables.TryGetValue(type, out var cachedObjects))
        {
            foreach (var cachedObject in cachedObjects)
            {
                yield return cachedObject;
            }
            
            yield break;
        }

        if (!serviceFactories.EnumerableDescriptors.TryGetValue(type, out var factories))
        {
            if(!type.IsGenericType || !serviceFactories.EnumerableDescriptors.TryGetValue(type.GetGenericTypeDefinition(), out factories))
            {
                yield break;
            }
        }
        
        if (!root.TryGetSingletons(type, out var singletons))
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
                item = Constructor.Construct(this, type, null, serviceDescriptor);
                
                (_forDisposal ??= []).Add(item);
            }
            
            cachedEnumerables.GetOrAdd(type, []).Add(item);

            yield return item;
        }
    }

    public object? GetService(Type type, string key)
    {
        if (_cachedKeyedObjects?.TryGetValue(type, out var dictionary) == true
            && dictionary.TryGetValue(key, out var cachedObject))
        {
            return cachedObject;
        }

        if (serviceFactories.KeyedDescriptor.TryGetValue(type, out var keyedDictionary)
            && keyedDictionary.TryGetValue(key, out var keyedDescriptor))
        {
            if (keyedDescriptor.Lifetime == Lifetime.Singleton)
            {
                return singletonScope.GetService(type, key);
            }
            
            var obj = Constructor.Construct(this, type, key, keyedDescriptor);
            
            (_cachedKeyedObjects ??= []).GetOrAdd(type, [])[key] = obj;
            (_forDisposal ??= []).Add(obj);

            return obj;
        }
        
        return GetServices(type, key).LastOrDefault();
    }

    public virtual IEnumerable<object> GetServices(Type type, string key)
    {
        var cachedKeyedEnumerables = _cachedKeyedEnumerables ??= [];
        if (cachedKeyedEnumerables.TryGetValue(type, out var dictionary)
            && dictionary.TryGetValue(key, out var cachedObjects))
        {
            foreach (var cachedObject in cachedObjects)
            {
                yield return cachedObject;
            }
            
            yield break;
        }

        if (!serviceFactories.KeyedEnumerableDescriptors.TryGetValue(type, out var keyedFactories)
            || !keyedFactories.TryGetValue(key, out var factories))
        {
            yield break;
        }
        
        if (!root.TryGetSingletons(type, key, out var singletons))
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
                item = Constructor.Construct(this, type, key, serviceDescriptor);
                
                (_forDisposal ??= []).Add(item);
            }
            
            cachedKeyedEnumerables.GetOrAdd(type, []).GetOrAdd(key, []).Add(item);

            yield return item;
        }
    }
    
    public virtual ValueTask DisposeAsync()
    {
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
                    return Await(i, vt, forDisposal);
                }
            }
            else if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        
        return default;
        
        static async ValueTask Await(int i, ValueTask vt, List<object> toDispose)
        {
            await vt.ConfigureAwait(false);
            // vt is acting on the disposable at index i,
            // decrement it and move to the next iteration
            i--;

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
        }
    }
}