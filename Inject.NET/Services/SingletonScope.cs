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

        var services = GetServices(serviceKey);

        if (services.Count == 0)
        {
            return ParentScope?.GetService(serviceKey, originatingScope);
        }

        // Check if any descriptors have predicates - if so, resolve conditionally
        if (serviceFactories.Descriptors.TryGetValue(serviceKey, out var descriptors))
        {
            var singletonDescriptors = descriptors.Items
                .Where(d => d.Lifetime == Lifetime.Singleton)
                .ToArray();

            if (singletonDescriptors.Any(d => d.Predicate != null))
            {
                var context = new ConditionalContext
                {
                    ServiceType = serviceKey.Type,
                    Key = serviceKey.Key
                };

                // Find matching descriptor from last to first (highest priority)
                for (var i = singletonDescriptors.Length - 1; i >= 0; i--)
                {
                    var candidate = singletonDescriptors[i];

                    if (candidate.Predicate == null || candidate.Predicate(context))
                    {
                        // Return the corresponding singleton instance
                        // The services list aligns with singletonDescriptors by index
                        if (i < services.Count)
                        {
                            return services[i];
                        }
                    }
                }

                return ParentScope?.GetService(serviceKey, originatingScope);
            }
        }

        return services[^1];
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

            // Create local singleton instances
            var singletonDescriptors = descriptors!.Items
                .Where(d => d.Lifetime == Lifetime.Singleton)
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