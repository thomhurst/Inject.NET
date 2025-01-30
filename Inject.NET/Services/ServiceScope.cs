using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using Inject.NET.Enums;
using Inject.NET.Extensions;
using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

public class ServiceScope<TSelf, TServiceProvider, TSingletonScope, TParentScope, TParentSingletonScope, TParentServiceProvider> : IServiceScope<TSelf, TServiceProvider, TSingletonScope>, IScoped
    where TServiceProvider : ServiceProvider<TServiceProvider, TSingletonScope, TSelf, TParentServiceProvider, TParentSingletonScope, TParentScope>, IServiceProvider<TSelf>
    where TSingletonScope : SingletonScope<TSingletonScope, TServiceProvider, TSelf, TParentSingletonScope, TParentScope, TParentServiceProvider>
    where TSelf : ServiceScope<TSelf, TServiceProvider, TSingletonScope, TParentScope, TParentSingletonScope, TParentServiceProvider>
    where TParentScope : IServiceScope
    where TParentSingletonScope : IServiceScope
{

#if NET9_0_OR_GREATER
    private bool _disposed;
#else
    private int _disposed;
#endif
    
    private Dictionary<ServiceKey, object>? _cachedObjects;
    private Dictionary<ServiceKey, List<object>>? _cachedEnumerables;
    
    private List<object>? _forDisposal;
    private readonly TServiceProvider _root;
    private readonly ServiceFactories _serviceFactories;

    public ServiceScope(TServiceProvider serviceProvider, ServiceFactories serviceFactories, TParentScope? parentScope)
    {
        _root = serviceProvider;
        _serviceFactories = serviceFactories;
        ParentScope = parentScope;
        Singletons = serviceProvider.Singletons;
        ServiceProvider = serviceProvider;
    }

    public TSingletonScope Singletons { get; }
    
    public TParentScope? ParentScope { get; }

    public TServiceProvider ServiceProvider { get; }

    public T Register<T>(ServiceKey key, T obj)
    {
        (_forDisposal ??= Pools.DisposalTracker.Get()).Add(obj!);

        return obj;
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

    public virtual object? GetService(ServiceKey serviceKey, IServiceScope originatingScope)
    {
        if (_cachedObjects?.TryGetValue(serviceKey, out var cachedObject) == true)
        {
            return cachedObject;
        }
        
        if (serviceKey.Type == Types.ServiceScope)
        {
            return this;
        }
        
        if (serviceKey.Type == Types.ServiceProvider)
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
                    return ParentScope?.GetService(serviceKey, originatingScope);
                }

                _serviceFactories.LateBoundGenericDescriptor[serviceKey] = descriptor;
            }
        }

        if (descriptor.Lifetime == Lifetime.Singleton)
        {
            return Singletons.GetService(serviceKey);
        }

        var obj = descriptor.Factory(originatingScope, serviceKey.Type, descriptor.Key);
            
        if(descriptor.Lifetime != Lifetime.Transient)
        {
            (_cachedObjects ??= Pools.Objects.Get())[serviceKey] = obj;
        }

        if(obj is IAsyncDisposable or IDisposable)
        {
            (_forDisposal ??= Pools.DisposalTracker.Get()).Add(obj);
        }

        return obj;
    }

    public IReadOnlyList<object> GetServices(ServiceKey serviceKey)
    {
        return GetServices(serviceKey, this);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public virtual IReadOnlyList<object> GetServices(ServiceKey serviceKey, IServiceScope originatingScope)
    {
        if (_cachedEnumerables?.TryGetValue(serviceKey, out var cachedObjects) == true)
        {
            return cachedObjects;
        }

        if (!_serviceFactories.Descriptors.TryGetValue(serviceKey, out var factories))
        {
            return ParentScope?.GetServices(serviceKey, originatingScope) ?? Array.Empty<object>();
        }
        
        if (!_root.TryGetSingletons(serviceKey, out var singletons))
        {
            singletons = Array.Empty<object>();
        }

        var cachedEnumerables = _cachedEnumerables ??= Pools.Enumerables.Get();

        return cachedEnumerables[serviceKey] = [..ConstructItems(factories, singletons, this, serviceKey, cachedEnumerables)];
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
                    (_forDisposal ??= Pools.DisposalTracker.Get()).Add(item);
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

        if(_cachedObjects != null)
        {
            Pools.Objects.Return(_cachedObjects);
        }

        if (_cachedEnumerables != null)
        {
            Pools.Enumerables.Return(_cachedEnumerables);
        }
        
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
        
        Pools.DisposalTracker.Return(forDisposal);
        
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
            
            Pools.DisposalTracker.Return(toDispose); 
        }
    }

    public void Dispose()
    {
        if(_cachedObjects != null)
        {
            Pools.Objects.Return(_cachedObjects);
        }

        if (_cachedEnumerables != null)
        {
            Pools.Enumerables.Return(_cachedEnumerables);
        }
        
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
        
        Pools.DisposalTracker.Return(forDisposal);
    }
}