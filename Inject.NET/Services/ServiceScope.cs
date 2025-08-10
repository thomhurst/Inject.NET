using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using Inject.NET.Enums;
using Inject.NET.Extensions;
using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

/// <summary>
/// Represents a service scope that manages the lifetime of scoped and transient services.
/// Provides service resolution, caching, and proper disposal patterns.
/// </summary>
/// <typeparam name="TSelf">The concrete service scope type</typeparam>
/// <typeparam name="TServiceProvider">The service provider type</typeparam>
/// <typeparam name="TSingletonScope">The singleton scope type</typeparam>
/// <typeparam name="TParentScope">The parent scope type</typeparam>
/// <typeparam name="TParentSingletonScope">The parent singleton scope type</typeparam>
/// <typeparam name="TParentServiceProvider">The parent service provider type</typeparam>
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

    /// <summary>
    /// Initializes a new instance of the service scope.
    /// </summary>
    /// <param name="serviceProvider">The parent service provider</param>
    /// <param name="serviceFactories">The service factories for creating instances</param>
    /// <param name="parentScope">The optional parent scope</param>
    public ServiceScope(TServiceProvider serviceProvider, ServiceFactories serviceFactories, TParentScope? parentScope)
    {
        _root = serviceProvider;
        _serviceFactories = serviceFactories;
        ParentScope = parentScope;
        Singletons = serviceProvider.Singletons;
        ServiceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets the singleton scope for accessing singleton services.
    /// </summary>
    public TSingletonScope Singletons { get; }
    
    /// <summary>
    /// Gets the parent scope, if this is a nested scope.
    /// </summary>
    public TParentScope? ParentScope { get; }

    /// <summary>
    /// Gets the service provider that created this scope.
    /// </summary>
    public TServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Registers an object for disposal when the scope is disposed.
    /// </summary>
    /// <typeparam name="T">The type of object to register</typeparam>
    /// <param name="obj">The object to register for disposal</param>
    /// <returns>The same object that was passed in</returns>
    public T Register<T>(T obj)
    {
        (_forDisposal ??= Pools.DisposalTracker.Get()).Add(obj!);

        return obj;
    }
    
    /// <summary>
    /// Gets a service instance of the specified type.
    /// </summary>
    /// <param name="type">The type of service to retrieve</param>
    /// <returns>The service instance, or null if not found</returns>
    public object? GetService(Type type)
    {
        var serviceKey = new ServiceKey(type);
        
        if (type.IsIEnumerable())
        {
            return GetServices(serviceKey);
        }
        
        return GetService(serviceKey);
    }

    /// <summary>
    /// Gets a service instance using the specified service key.
    /// </summary>
    /// <param name="serviceKey">The service key containing type and optional key information</param>
    /// <returns>The service instance, or null if not found</returns>
    public object? GetService(ServiceKey serviceKey)
    {
        return GetService(serviceKey, this);
    }

    /// <summary>
    /// Gets a service instance using the specified service key and originating scope.
    /// This method handles service resolution with proper scope chain traversal.
    /// </summary>
    /// <param name="serviceKey">The service key containing type and optional key information</param>
    /// <param name="originatingScope">The scope that initiated the service request</param>
    /// <returns>The service instance, or null if not found</returns>
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
        
        if (serviceKey.Type == Types.ServiceProvider || serviceKey.Type == Types.SystemServiceProvider)
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

    /// <summary>
    /// Gets all service instances matching the specified service key.
    /// </summary>
    /// <param name="serviceKey">The service key containing type and optional key information</param>
    /// <returns>A read-only list of all matching service instances</returns>
    public IReadOnlyList<object> GetServices(ServiceKey serviceKey)
    {
        return GetServices(serviceKey, this);
    }
    
    /// <summary>
    /// Gets all service instances matching the specified service key and originating scope.
    /// This method handles collection resolution with proper scope chain traversal.
    /// </summary>
    /// <param name="serviceKey">The service key containing type and optional key information</param>
    /// <param name="originatingScope">The scope that initiated the service request</param>
    /// <returns>A read-only list of all matching service instances</returns>
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

    /// <summary>
    /// Asynchronously disposes the service scope and all tracked disposable objects.
    /// Objects are disposed in reverse order of their registration.
    /// </summary>
    /// <returns>A task representing the disposal operation</returns>
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
        
        if(ParentScope?.DisposeAsync() is { IsCompleted: false } valueTask)
        {
            return Await(forDisposal.Count, valueTask, forDisposal);
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

    /// <summary>
    /// Disposes the service scope and all tracked disposable objects.
    /// Objects are disposed in reverse order of their registration.
    /// </summary>
    public void Dispose()
    {
        ParentScope?.Dispose();
        
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