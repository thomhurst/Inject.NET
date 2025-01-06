using Inject.NET.Enums;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using Inject.NET.Pools;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal sealed class ServiceScope(ServiceProviderRoot root, IServiceScope singletonScope, ServiceFactories serviceFactories)
    : IServiceScope
{
    private static readonly Type ServiceScopeType = typeof(IServiceScope);
    private static readonly Type ServiceProviderType = typeof(IServiceProvider);
    
    private Dictionary<ServiceKey, object>? _cachedObjects;
    private Dictionary<ServiceKey, List<object>>? _cachedEnumerables;
    
    private List<object>? _forDisposal;
    
    public IServiceProvider ServiceProvider { get; } = root;
    
    public object? GetService(Type type)
    {
        return GetService(new ServiceKey(type));
    }

    public object? GetService(ServiceKey serviceKey)
    {
        return GetService(serviceKey, this);
    }

    public object? GetService(ServiceKey serviceKey, IServiceScope scope)
    {
        if (serviceKey.Type == ServiceScopeType)
        {
            return this;
        }
        
        if (serviceKey.Type == ServiceProviderType)
        {
            return ServiceProvider;
        }
        
        if (_cachedObjects?.TryGetValue(serviceKey, out var cachedObject) == true)
        {
            return cachedObject;
        }
        
        if (serviceFactories.Descriptor.TryGetValue(serviceKey, out var descriptor))
        {
            if (descriptor.Lifetime == Lifetime.Singleton)
            {
                return singletonScope.GetService(serviceKey);
            }
            
            var obj = Constructor.Construct(this, serviceKey.Type, descriptor);
            
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
        
        return GetServices(serviceKey).LastOrDefault();
    }

    public IEnumerable<object> GetServices(ServiceKey serviceKey)
    {
        return GetServices(serviceKey, this);
    }

    public IEnumerable<object> GetServices(ServiceKey serviceKey, IServiceScope serviceScope)
    {
        if (_cachedEnumerables?.TryGetValue(serviceKey, out var cachedObjects) == true)
        {
            foreach (var cachedObject in cachedObjects)
            {
                yield return cachedObject;
            }
            
            yield break;
        }

        if (!serviceFactories.Descriptors.TryGetValue(serviceKey, out var factories))
        {
            yield break;
        }
        
        if (!root.TryGetSingletons(serviceKey, out var singletons))
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
                item = Constructor.Construct(this, serviceKey.Type, serviceDescriptor);
                
                if(item is IAsyncDisposable or IDisposable)
                {
                    (_forDisposal ??= ListPool<object>.Shared.Get()).Add(item);
                }
            }
            
            if(lifetime != Lifetime.Transient)
            {
                var cachedEnumerables = _cachedEnumerables ??= DictionaryPool<ServiceKey, List<object>>.Shared.Get();

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
                _ = asyncDisposable.DisposeAsync();
            }
        }
        
        ListPool<object>.Shared.Return(forDisposal);
    }
}