namespace Inject.NET.Interfaces;

/// <summary>
/// Defines a custom lifetime scope that controls when service instances are created and reused.
/// Implementations cache service instances based on custom lifetime rules (e.g., per-thread, per-resolution-graph).
/// </summary>
public interface ILifetimeScope : IDisposable
{
    /// <summary>
    /// Gets an existing service instance for the specified key, or creates a new one using the factory.
    /// Thread safety requirements depend on the implementation.
    /// </summary>
    /// <param name="serviceType">The service type used as the cache key</param>
    /// <param name="key">The optional service key for keyed services</param>
    /// <param name="factory">A factory function that creates the service instance when not cached</param>
    /// <returns>The cached or newly created service instance</returns>
    object GetOrCreate(Type serviceType, string? key, Func<object> factory);
}
