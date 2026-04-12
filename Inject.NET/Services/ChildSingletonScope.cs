using System.Collections.Concurrent;
using Inject.NET.Enums;
using Inject.NET.Extensions;
using Inject.NET.Helpers;
using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

/// <summary>
/// Manages singleton services for a child container. For service keys that are overridden
/// in the child, new singleton instances are created and tracked. For inherited (non-overridden)
/// service keys, resolution is delegated to the parent's scopes so the same parent-owned
/// instance is reused.
/// </summary>
internal sealed class ChildSingletonScope : IServiceScope, ISingleton
{
    private readonly ChildServiceProvider _serviceProvider;
    private readonly ServiceFactories _mergedFactories;
    private readonly ConcurrentDictionary<ServiceKey, object?> _singletonCache = new();
    private readonly ConcurrentDictionary<ServiceKey, IReadOnlyList<object>> _singletonCollectionCache = new();
    private readonly List<object> _constructedObjects = [];
    private readonly ConcurrentDictionary<ServiceKey, object> _compositeCache = new();

    internal ChildSingletonScope(ChildServiceProvider serviceProvider, ServiceFactories mergedFactories)
    {
        _serviceProvider = serviceProvider;
        _mergedFactories = mergedFactories;
    }

    public object? GetService(Type type)
    {
        var serviceKey = new ServiceKey(type);

        if (type.IsIEnumerable())
        {
            var elementType = type.GetGenericArguments()[0];
            return GetServices(serviceKey with { Type = elementType });
        }

        return GetService(serviceKey);
    }

    public object? GetService(ServiceKey serviceKey)
    {
        return GetService(serviceKey, this);
    }

    public object? GetService(ServiceKey serviceKey, IServiceScope originatingScope)
    {
        if (serviceKey.Type == Types.ServiceScope)
        {
            return this;
        }

        if (serviceKey.Type == Types.ServiceProvider || serviceKey.Type == Types.SystemServiceProvider)
        {
            return _serviceProvider;
        }

        // Check if there's a composite descriptor for this service key
        if (_mergedFactories.Descriptor.TryGetValue(serviceKey, out var descriptor) && descriptor.IsComposite)
        {
            return _compositeCache.GetOrAdd(serviceKey, (key, state) =>
            {
                var (self, desc, scope) = state;
                var obj = desc.Factory(scope, key.Type, desc.Key);
                lock (self._constructedObjects)
                {
                    self._constructedObjects.Add(obj);
                }
                return obj;
            }, (this, descriptor, originatingScope));
        }

        var services = GetServices(serviceKey);

        if (services.Count == 0)
        {
            return null;
        }

        return services[^1];
    }

    public IReadOnlyList<object> GetServices(ServiceKey serviceKey)
    {
        return GetServices(serviceKey, this);
    }

    public IReadOnlyList<object> GetServices(ServiceKey serviceKey, IServiceScope originatingScope)
    {
        // If this child overrides the key, build (and cache) local singletons.
        if (_serviceProvider.HasChildOverride(serviceKey))
        {
            return BuildLocalSingletons(serviceKey, originatingScope);
        }

        // Otherwise delegate to parent so inherited singletons are the same instances
        // that the parent already constructed (supports multi-tenancy with shared defaults).
        if (_serviceProvider.Parent is IInternalSingletonResolver parentResolver)
        {
            return parentResolver.ResolveSingletons(serviceKey);
        }

        return [];
    }

    /// <summary>
    /// Builds (or returns a cached) list of singleton instances owned by THIS child container
    /// for the given key. Honors ExternallyOwned so MEDI-style pre-built instances are tracked
    /// for disposal when the child owns them.
    /// </summary>
    internal IReadOnlyList<object> BuildLocalSingletons(ServiceKey serviceKey)
    {
        return BuildLocalSingletons(serviceKey, this);
    }

    private IReadOnlyList<object> BuildLocalSingletons(ServiceKey serviceKey, IServiceScope originatingScope)
    {
        return _singletonCollectionCache.GetOrAdd(serviceKey, (key, state) =>
        {
            var (self, factories, originScope) = state;

            if (!factories.Descriptors.TryGetValue(key, out var descriptors))
            {
                // Open-generic fallback: if the closed generic has no direct registration,
                // look up the open type definition so AddSingleton(typeof(IRepo<>), typeof(Repo<>))
                // resolves for e.g. IRepo<int>.
                if (key.Type.IsConstructedGenericType)
                {
                    var openKey = key with { Type = key.Type.GetGenericTypeDefinition() };
                    if (!factories.Descriptors.TryGetValue(openKey, out descriptors))
                    {
                        return [];
                    }
                }
                else
                {
                    return [];
                }
            }

            var singletonDescriptors = descriptors.Items
                .Where(d => d.Lifetime == Lifetime.Singleton && !d.IsComposite)
                .ToList();

            if (singletonDescriptors.Count == 0)
            {
                return [];
            }

            var results = new List<object>(singletonDescriptors.Count);

            foreach (var descriptor in singletonDescriptors)
            {
                // Pass the CLOSED generic type so the factory can call MakeGenericType.
                var obj = descriptor.Factory(originScope, key.Type, descriptor.Key);

                if (!descriptor.ExternallyOwned)
                {
                    lock (self._constructedObjects)
                    {
                        self._constructedObjects.Add(obj);
                    }
                }

                results.Add(obj);
            }

            return (IReadOnlyList<object>)results.AsReadOnly();
        }, (this, _mergedFactories, originatingScope));
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var item in _constructedObjects)
        {
            await Disposer.DisposeAsync(item);
        }
    }

    public void Dispose()
    {
        foreach (var item in _constructedObjects)
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
