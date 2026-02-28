using System.Collections.Frozen;
using Inject.NET.Enums;
using Inject.NET.Extensions;
using Inject.NET.Helpers;
using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

/// <summary>
/// A service scope for a child container. Resolves services using the merged factories
/// (parent + child overrides). Singletons are resolved from the child's singleton scope,
/// while scoped and transient services are created fresh per scope.
/// </summary>
internal sealed class ChildServiceScope : IServiceScope
{
    private readonly ChildServiceProvider _serviceProvider;
    private readonly ServiceFactories _serviceFactories;
    private readonly ChildSingletonScope _singletons;

    private Dictionary<ServiceKey, object>? _cachedObjects;
    private Dictionary<ServiceKey, List<object>>? _cachedEnumerables;
    private List<object>? _forDisposal;
    private bool _disposed;

    internal ChildServiceScope(ChildServiceProvider serviceProvider, ServiceFactories serviceFactories)
    {
        _serviceProvider = serviceProvider;
        _serviceFactories = serviceFactories;
        _singletons = serviceProvider.Singletons;
    }

    public object? GetService(Type type)
    {
        var serviceKey = new ServiceKey(type);

        if (type.IsIEnumerable())
        {
            var elementType = type.GetGenericArguments()[0];
            return GetServices(serviceKey with { Type = elementType });
        }

        // Handle Lazy<T>
        if (type.IsLazy())
        {
            var innerType = type.GetGenericArguments()[0];
            var lazyFactory = typeof(LazyFactory<>).MakeGenericType(innerType);
            var factory = Activator.CreateInstance(lazyFactory, this);
            return lazyFactory.GetMethod("Create")!.Invoke(factory, null);
        }

        // Handle Func<T>
        if (type.IsFunc())
        {
            var innerType = type.GetGenericArguments()[0];
            return CreateFuncFactory(innerType);
        }

        return GetService(serviceKey);
    }

    private object CreateFuncFactory(Type innerType)
    {
        var serviceKey = new ServiceKey(innerType);
        var scope = this;
        Func<object?> objectFactory = () => scope.GetService(serviceKey);

        var createMethod = typeof(ChildServiceScope)
            .GetMethod(nameof(WrapFuncFactory), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(innerType);
        return createMethod.Invoke(null, [objectFactory])!;
    }

    private static Func<T> WrapFuncFactory<T>(Func<object?> factory)
    {
        return () => (T)factory()!;
    }

    public object? GetService(ServiceKey serviceKey)
    {
        return GetService(serviceKey, this);
    }

    public object? GetService(ServiceKey serviceKey, IServiceScope originatingScope)
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
            return _serviceProvider;
        }

        // Check if requesting IEnumerable<T>
        if (serviceKey.Type.IsIEnumerable())
        {
            var elementType = serviceKey.Type.GetGenericArguments()[0];
            return GetServices(serviceKey with { Type = elementType });
        }

        // Handle Func<T>
        if (serviceKey.Type.IsFunc())
        {
            var innerType = serviceKey.Type.GetGenericArguments()[0];
            return CreateFuncFactory(innerType);
        }

        // Look up the descriptor
        ServiceDescriptor? descriptor = null;
        if (!_serviceFactories.Descriptor.TryGetValue(serviceKey, out descriptor))
        {
            if (!_serviceFactories.LateBoundGenericDescriptor.TryGetValue(serviceKey, out descriptor))
            {
                if (!serviceKey.Type.IsGenericType ||
                    !_serviceFactories.Descriptor.TryGetValue(
                        serviceKey with { Type = serviceKey.Type.GetGenericTypeDefinition() }, out descriptor))
                {
                    // No descriptor found
                    if (_cachedEnumerables?.TryGetValue(serviceKey, out var cachedEnumerable) == true
                        && cachedEnumerable.Count > 0)
                    {
                        return cachedEnumerable[^1];
                    }

                    return null;
                }

                _serviceFactories.LateBoundGenericDescriptor[serviceKey] = descriptor;
            }
        }

        // Resolve conditional descriptors
        descriptor = ResolveConditionalDescriptor(serviceKey, descriptor);

        if (descriptor == null)
        {
            return null;
        }

        // For non-composite services, check cached enumerables as optimization
        if (!descriptor.IsComposite
            && _cachedEnumerables?.TryGetValue(serviceKey, out var cached) == true
            && cached.Count > 0)
        {
            return cached[^1];
        }

        if (descriptor.Lifetime == Lifetime.Singleton)
        {
            return _singletons.GetService(serviceKey);
        }

        var obj = descriptor.Factory(originatingScope, serviceKey.Type, descriptor.Key);

        if (descriptor.Lifetime != Lifetime.Transient)
        {
            (_cachedObjects ??= new())[serviceKey] = obj;
        }

        if (!descriptor.ExternallyOwned && obj is IAsyncDisposable or IDisposable)
        {
            (_forDisposal ??= []).Add(obj);
        }

        return obj;
    }

    private ServiceDescriptor? ResolveConditionalDescriptor(ServiceKey serviceKey, ServiceDescriptor defaultDescriptor)
    {
        if (defaultDescriptor.Predicate == null)
        {
            return defaultDescriptor;
        }

        if (!_serviceFactories.Descriptors.TryGetValue(serviceKey, out var descriptors))
        {
            return defaultDescriptor;
        }

        var context = new ConditionalContext
        {
            ServiceType = serviceKey.Type,
            Key = serviceKey.Key
        };

        for (var i = descriptors.Items.Length - 1; i >= 0; i--)
        {
            var candidate = descriptors.Items[i];

            if (candidate.Predicate == null)
            {
                return candidate;
            }

            if (candidate.Predicate(context))
            {
                return candidate;
            }
        }

        return null;
    }

    public IReadOnlyList<object> GetServices(ServiceKey serviceKey)
    {
        return GetServices(serviceKey, this);
    }

    public IReadOnlyList<object> GetServices(ServiceKey serviceKey, IServiceScope originatingScope)
    {
        if (_cachedEnumerables?.TryGetValue(serviceKey, out var cachedObjects) == true)
        {
            return cachedObjects;
        }

        if (!_serviceFactories.Descriptors.TryGetValue(serviceKey, out var factories))
        {
            return Array.Empty<object>();
        }

        var singletons = _singletons.GetServices(serviceKey);

        var cachedEnumerables = _cachedEnumerables ??= new();

        return cachedEnumerables[serviceKey] = [..ConstructItems(factories, singletons, originatingScope, serviceKey, cachedEnumerables)];
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

            if (serviceDescriptor.IsComposite)
            {
                if (lifetime == Lifetime.Singleton)
                {
                    singletonIndex++;
                }
                continue;
            }

            object? item;
            if (lifetime == Lifetime.Singleton)
            {
                item = singletons[singletonIndex++];
            }
            else
            {
                item = serviceDescriptor.Factory(scope, serviceKey.Type, serviceDescriptor.Key);

                if (!serviceDescriptor.ExternallyOwned && item is IAsyncDisposable or IDisposable)
                {
                    (_forDisposal ??= []).Add(item);
                }
            }

            if (lifetime != Lifetime.Transient)
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

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_forDisposal is not { } forDisposal)
        {
            return;
        }

        for (var i = forDisposal.Count - 1; i >= 0; i--)
        {
            var obj = forDisposal[i];

            if (obj is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_forDisposal is not { } forDisposal)
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
            else if (toDispose is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
