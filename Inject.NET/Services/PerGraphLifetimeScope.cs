using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

/// <summary>
/// A lifetime scope that shares instances within a single resolution graph.
/// All services resolved during a single <see cref="BeginScope"/> / <see cref="EndScope"/> block
/// share the same instances. Separate resolution graphs get separate instances.
/// </summary>
/// <remarks>
/// Uses <see cref="AsyncLocal{T}"/> to track the current resolution graph, making it safe
/// for use with async/await patterns. The resolution graph is scoped to the current
/// async execution context.
/// </remarks>
public sealed class PerGraphLifetimeScope : ILifetimeScope
{
    private readonly AsyncLocal<Dictionary<ServiceKey, object>?> _currentGraph = new();
    private readonly object _lock = new();
    private readonly List<Dictionary<ServiceKey, object>> _allGraphs = [];
    private volatile bool _disposed;

    /// <summary>
    /// Begins a new resolution graph scope. All service resolutions within this scope
    /// will share instances. Must be paired with a call to <see cref="EndScope"/>.
    /// </summary>
    /// <remarks>
    /// Typically called at the start of a top-level resolution to establish a graph context.
    /// Nested calls to <see cref="BeginScope"/> will not create a new graph if one is already active.
    /// </remarks>
    public void BeginScope()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_currentGraph.Value != null)
        {
            // Nested resolution - reuse existing graph
            return;
        }

        var graph = new Dictionary<ServiceKey, object>();
        _currentGraph.Value = graph;

        lock (_lock)
        {
            _allGraphs.Add(graph);
        }
    }

    /// <summary>
    /// Ends the current resolution graph scope, clearing the graph context.
    /// </summary>
    public void EndScope()
    {
        var graph = _currentGraph.Value;

        if (graph != null)
        {
            _currentGraph.Value = null;

            lock (_lock)
            {
                _allGraphs.Remove(graph);
            }
        }
    }

    /// <inheritdoc />
    public object GetOrCreate(Type serviceType, string? key, Func<object> factory)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var graph = _currentGraph.Value;

        if (graph == null)
        {
            // No active resolution graph - auto-create one for this single resolution
            // This handles the case where GetOrCreate is called without an explicit BeginScope
            return factory();
        }

        var serviceKey = new ServiceKey(serviceType, key);

        if (graph.TryGetValue(serviceKey, out var existing))
        {
            return existing;
        }

        var instance = factory();
        graph[serviceKey] = instance;
        return instance;
    }

    /// <summary>
    /// Disposes all cached instances across all graphs and clears the async-local state.
    /// Instances that implement <see cref="IDisposable"/> are disposed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        List<Dictionary<ServiceKey, object>> graphsCopy;

        lock (_lock)
        {
            graphsCopy = [.. _allGraphs];
            _allGraphs.Clear();
        }

        foreach (var graph in graphsCopy)
        {
            foreach (var instance in graph.Values)
            {
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            graph.Clear();
        }

        _currentGraph.Value = null;
    }
}
