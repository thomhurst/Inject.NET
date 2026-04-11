using System.Collections.Concurrent;
using System.Collections.Frozen;
using Inject.NET.Enums;
using Inject.NET.Extensions;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

/// <summary>
/// A child service provider that inherits registrations from a parent provider
/// but can add or override registrations independently.
/// Child singletons are independent from the parent, and scoped/transient instances
/// are always created fresh within the child's scopes.
/// </summary>
public sealed class ChildServiceProvider : IServiceProvider, IInternalSingletonResolver, IAsyncDisposable
{
    private readonly IServiceProvider _parent;
    private readonly ServiceFactories _childFactories;
    private readonly ServiceFactories _mergedFactories;
    private readonly ChildSingletonScope _singletonScope;
    private readonly ConcurrentBag<IAsyncDisposable> _childContainers = [];
    private bool _disposed;

    internal ChildServiceProvider(IServiceProvider parent, ServiceFactories parentFactories, ServiceFactoryBuilders childOverrides)
    {
        _parent = parent;
        _childFactories = childOverrides.AsReadOnly();
        _mergedFactories = MergeFactories(parentFactories, _childFactories);
        _singletonScope = new ChildSingletonScope(this, _mergedFactories);
    }

    /// <summary>
    /// Gets the parent service provider.
    /// </summary>
    public IServiceProvider Parent => _parent;

    /// <summary>
    /// Gets the singleton scope for this child provider.
    /// </summary>
    internal ChildSingletonScope Singletons => _singletonScope;

    /// <summary>
    /// Gets the merged service factories (parent + child overrides).
    /// </summary>
    internal ServiceFactories ServiceFactories => _mergedFactories;

    /// <summary>
    /// Gets the child-only factories (overrides/additions).
    /// </summary>
    internal ServiceFactories ChildFactories => _childFactories;

    /// <inheritdoc />
    public IServiceScope CreateScope()
    {
        return new ChildServiceScope(this, _mergedFactories);
    }

    /// <inheritdoc />
    public bool IsService(Type serviceType)
    {
        if (serviceType == Types.ServiceScope
            || serviceType == Types.ServiceProvider
            || serviceType == Types.SystemServiceProvider)
        {
            return true;
        }

        var serviceKey = new ServiceKey(serviceType);

        if (_mergedFactories.Descriptors.ContainsKey(serviceKey))
        {
            return true;
        }

        if (serviceType.IsConstructedGenericType
            && _mergedFactories.Descriptors.ContainsKey(new ServiceKey(serviceType.GetGenericTypeDefinition())))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType)
    {
        using var scope = new ChildServiceScope(this, _mergedFactories);
        return scope.GetService(serviceType);
    }

    /// <summary>
    /// Creates a nested child container that inherits this child's merged registrations
    /// and can add or override registrations further.
    /// </summary>
    /// <param name="configure">An action to configure additional or overriding registrations</param>
    /// <returns>A new child service provider</returns>
    public ChildServiceProvider CreateChildContainer(Action<IServiceRegistrar> configure)
    {
        var registrar = new ChildContainerRegistrar();
        configure(registrar);

        var child = new ChildServiceProvider(this, _mergedFactories, registrar.ServiceFactoryBuilders);
        _childContainers.Add(child);
        return child;
    }

    /// <summary>
    /// Checks whether the specified service type has been overridden in this child container.
    /// For closed generic types, also checks whether the open generic type definition is
    /// overridden, so open-generic registrations are honored for their closed resolutions.
    /// </summary>
    internal bool HasChildOverride(ServiceKey serviceKey)
    {
        if (_childFactories.Descriptors.ContainsKey(serviceKey))
        {
            return true;
        }

        if (serviceKey.Type.IsConstructedGenericType)
        {
            var openKey = serviceKey with { Type = serviceKey.Type.GetGenericTypeDefinition() };
            if (_childFactories.Descriptors.ContainsKey(openKey))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Resolves singleton instances for the given key. If this child overrides the key,
    /// builds (or reuses cached) child-owned instances. Otherwise, delegates to the parent
    /// provider so the same parent-owned instance is returned for inherited keys.
    /// </summary>
    IReadOnlyList<object> IInternalSingletonResolver.ResolveSingletons(ServiceKey serviceKey)
    {
        if (HasChildOverride(serviceKey))
        {
            return _singletonScope.BuildLocalSingletons(serviceKey);
        }

        if (_parent is IInternalSingletonResolver parentResolver)
        {
            return parentResolver.ResolveSingletons(serviceKey);
        }

        return Array.Empty<object>();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Dispose child containers first (nested children before this one)
        while (_childContainers.TryTake(out var child))
        {
            await child.DisposeAsync();
        }

        // Dispose our own singletons
        await _singletonScope.DisposeAsync();
    }

    /// <summary>
    /// Merges parent factories with child overrides. For each service key, if the child
    /// defines it, the child's registration wins; otherwise the parent's is used.
    /// </summary>
    private static ServiceFactories MergeFactories(ServiceFactories parentFactories, ServiceFactories childFactories)
    {
        var merged = new Dictionary<ServiceKey, FrozenSet<ServiceDescriptor>>();

        // Start with all parent registrations
        foreach (var (key, descriptors) in parentFactories.Descriptors)
        {
            merged[key] = descriptors;
        }

        // Override/add with child registrations
        foreach (var (key, descriptors) in childFactories.Descriptors)
        {
            merged[key] = descriptors;
        }

        return new ServiceFactories(merged.ToFrozenDictionary());
    }
}
