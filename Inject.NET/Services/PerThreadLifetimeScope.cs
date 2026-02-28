using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

/// <summary>
/// A lifetime scope that maintains one instance per thread.
/// Each thread that requests a service gets its own instance, which is reused on subsequent
/// requests from the same thread.
/// </summary>
/// <remarks>
/// Thread-local storage ensures that each thread has its own dictionary of instances.
/// Disposal disposes all cached instances across all threads.
/// </remarks>
public sealed class PerThreadLifetimeScope : ILifetimeScope
{
    private readonly ThreadLocal<Dictionary<ServiceKey, object>> _instances =
        new(() => new Dictionary<ServiceKey, object>(), trackAllValues: true);

    private volatile bool _disposed;

    /// <inheritdoc />
    public object GetOrCreate(Type serviceType, string? key, Func<object> factory)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var serviceKey = new ServiceKey(serviceType, key);
        var dict = _instances.Value!;

        if (dict.TryGetValue(serviceKey, out var existing))
        {
            return existing;
        }

        var instance = factory();
        dict[serviceKey] = instance;
        return instance;
    }

    /// <summary>
    /// Disposes all cached instances across all threads and releases the thread-local storage.
    /// Instances that implement <see cref="IDisposable"/> are disposed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var dict in _instances.Values)
        {
            foreach (var instance in dict.Values)
            {
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            dict.Clear();
        }

        _instances.Dispose();
    }
}
