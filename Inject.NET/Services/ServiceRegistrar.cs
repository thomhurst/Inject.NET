using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

/// <summary>
/// Base class for service registrars that manage service registration and provider building.
/// Enables tenant-aware service registration and provider construction.
/// </summary>
/// <typeparam name="TServiceProvider">The service provider type to build</typeparam>
/// <typeparam name="TParentServiceProvider">The parent service provider type</typeparam>
public abstract class ServiceRegistrar<TServiceProvider, TParentServiceProvider> : ITenantedServiceRegistrar<TServiceProvider, TParentServiceProvider> where TServiceProvider : IServiceProvider
{
    /// <summary>
    /// Gets the service factory builders that collect service descriptors for provider construction.
    /// </summary>
    public ServiceFactoryBuilders ServiceFactoryBuilders { get; } = new();

    /// <summary>
    /// Registers a service descriptor with the registrar.
    /// </summary>
    /// <param name="serviceDescriptor">The service descriptor containing type, implementation, and lifetime information</param>
    /// <returns>The current registrar instance for fluent configuration</returns>
    public ITenantedServiceRegistrar<TServiceProvider, TParentServiceProvider> Register(ServiceDescriptor serviceDescriptor)
    {
        ServiceFactoryBuilders.Add(serviceDescriptor);

        return this;
    }

    /// <summary>
    /// Explicit implementation of non-generic interface for extension method compatibility.
    /// </summary>
    IServiceRegistrar IServiceRegistrar.Register(ServiceDescriptor serviceDescriptor)
    {
        return Register(serviceDescriptor);
    }
    
    /// <summary>
    /// Builds a service provider instance from the registered service descriptors.
    /// </summary>
    /// <param name="parentServiceProvider">The optional parent service provider for hierarchical dependency injection</param>
    /// <returns>A task representing the asynchronous build operation that returns the service provider</returns>
    public abstract ValueTask<TServiceProvider> BuildAsync(TParentServiceProvider? parentServiceProvider);
}