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
public sealed class ChildServiceProvider : IServiceProvider, IAsyncDisposable
{
    private readonly IServiceProvider _parent;
    private readonly ServiceFactories _childFactories;
    private readonly ServiceFactories _mergedFactories;
    private readonly ChildSingletonScope _singletonScope;
    private readonly List<IAsyncDisposable> _childContainers = [];
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
    /// </summary>
    internal bool HasChildOverride(ServiceKey serviceKey)
    {
        return _childFactories.Descriptors.ContainsKey(serviceKey);
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
        foreach (var child in _childContainers)
        {
            await child.DisposeAsync();
        }

        _childContainers.Clear();

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
