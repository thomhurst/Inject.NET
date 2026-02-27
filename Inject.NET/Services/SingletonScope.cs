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

        if (serviceKey.Type == Types.ServiceProviderIsService)
        {
            return ServiceProvider;
        }

        if (serviceKey.Type == Types.ServiceScopeFactory)
        {
            return ServiceProvider;
        }

        // Check if there's a composite descriptor for this service key
        // If so, resolve the composite directly instead of going through GetServices
        // (which excludes composites from enumerable resolution)
        if (serviceFactories.Descriptor.TryGetValue(serviceKey, out var descriptor) && descriptor.IsComposite)
        {
            return GetOrCreateComposite(serviceKey, descriptor, originatingScope);
        }

        var services = GetServices(serviceKey);

        if (services.Count == 0)
        {
            return ParentScope?.GetService(serviceKey, originatingScope);
        }

        return services[^1];
    }

    private readonly ConcurrentDictionary<ServiceKey, object> _compositeCache = new();

    private object GetOrCreateComposite(ServiceKey serviceKey, ServiceDescriptor descriptor, IServiceScope originatingScope)
    {
        return _compositeCache.GetOrAdd(serviceKey, (key, state) =>
        {
            var (self, desc, originScope) = state;
            var obj = desc.Factory(originScope, key.Type, desc.Key);
            self.Register(obj);
            return obj;
        }, (this, descriptor, originatingScope));
    }

    public virtual IReadOnlyList<object> GetServices(ServiceKey serviceKey, IServiceScope originatingScope)
    {
        return _singletonCollectionCache.GetOrAdd(serviceKey, (key, state) =>
        {
            var (self, factories, originScope, parentScope) = state;

            // Check if this service is defined locally
            var hasLocalDefinition = factories.Descriptors.TryGetValue(key, out var descriptors) &&
                                     descriptors.Items.Any(d => d.Lifetime == Lifetime.Singleton);

            // If no local definition and we have a parent, delegate to parent
            if (!hasLocalDefinition && parentScope != null)
            {
                return parentScope.GetServices(key, originScope);
            }

            // No services if no local definition and no parent
            if (!hasLocalDefinition)
            {
                return [];
            }

            // Create local singleton instances, excluding composites from enumerable resolution
            var singletonDescriptors = descriptors!.Items
                .Where(d => d.Lifetime == Lifetime.Singleton && !d.IsComposite)
                .ToList();

            var results = new List<object>(singletonDescriptors.Count);

            foreach (var descriptor in singletonDescriptors)
            {
                var obj = descriptor.Factory(originScope, key.Type, descriptor.Key);
                if (!descriptor.ExternallyOwned)
                {
                    self.Register(obj);
                }
                results.Add(obj);
            }

            return (IReadOnlyList<object>)results.AsReadOnly();
        }, (this, serviceFactories, originatingScope, ParentScope));
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