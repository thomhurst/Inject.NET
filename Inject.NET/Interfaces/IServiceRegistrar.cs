using Inject.NET.Models;

namespace Inject.NET.Interfaces;

/// <summary>
/// Non-generic interface for service registration.
/// Provides a simple extension point for fluent registration APIs without requiring generic type parameters.
/// </summary>
public interface IServiceRegistrar
{
    /// <summary>
    /// Gets the service factory builders that collect service descriptors for provider construction.
    /// </summary>
    ServiceFactoryBuilders ServiceFactoryBuilders { get; }

    /// <summary>
    /// Registers a service descriptor with the registrar.
    /// </summary>
    /// <param name="descriptor">The service descriptor containing type, implementation, and lifetime information</param>
    /// <returns>The current registrar instance for fluent configuration</returns>
    IServiceRegistrar Register(ServiceDescriptor descriptor);
}
