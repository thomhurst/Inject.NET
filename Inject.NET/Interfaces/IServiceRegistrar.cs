using Inject.NET.Models;

namespace Inject.NET.Interfaces;

/// <summary>
/// Represents a service registrar that manages service registration and provider building for dependency injection.
/// Supports tenant-aware service registration and hierarchical service provider construction.
/// </summary>
/// <typeparam name="TSelf">The concrete service registrar type</typeparam>
/// <typeparam name="TRootServiceProvider">The root service provider type</typeparam>
/// <typeparam name="TTenantServiceProvider">The tenant-specific service provider type</typeparam>
public interface IServiceRegistrar<out TSelf, in TRootServiceProvider, TTenantServiceProvider>
where TSelf : IServiceRegistrar<TSelf, TRootServiceProvider, TTenantServiceProvider>
where TRootServiceProvider : IServiceProvider
where TTenantServiceProvider : IServiceProvider
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
    TSelf Register(ServiceDescriptor descriptor);
    
    /// <summary>
    /// Builds a tenant-specific service provider instance from the registered service descriptors.
    /// </summary>
    /// <param name="rootServiceProvider">The root service provider for hierarchical dependency injection</param>
    /// <returns>A task representing the asynchronous build operation that returns the tenant service provider</returns>
    ValueTask<TTenantServiceProvider> BuildAsync(TRootServiceProvider rootServiceProvider);
}