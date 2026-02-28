using Inject.NET.Enums;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using Inject.NET.Services;

namespace Inject.NET.Extensions;

/// <summary>
/// Extension methods for registering services with custom lifetime scopes.
/// Provides PerThread, PerGraph, and user-defined lifetime registrations.
/// </summary>
public static class CustomLifetimeExtensions
{
    // ═══════════════════════════════════════════════════════════════════════════════════
    // PER-THREAD REGISTRATIONS
    // One instance per thread, reused within the same thread
    // ═══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registers a service with per-thread lifetime using a new <see cref="PerThreadLifetimeScope"/>.
    /// Each thread gets its own instance; the same thread always receives the same instance.
    /// </summary>
    /// <typeparam name="TService">The service type (interface or base class)</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <returns>The registrar for fluent chaining</returns>
    /// <example>
    /// <code>
    /// partial void ConfigureServices()
    /// {
    ///     this.AddPerThread&lt;IService, Service&gt;();
    /// }
    /// </code>
    /// </example>
    public static IServiceRegistrar AddPerThread<TService, TImplementation>(
        this IServiceRegistrar registrar)
        where TService : class
        where TImplementation : class, TService
    {
        var scope = new PerThreadLifetimeScope();
        return registrar.AddWithLifetime<TService, TImplementation>(scope);
    }

    /// <summary>
    /// Registers a service as itself with per-thread lifetime.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <returns>The registrar for fluent chaining</returns>
    public static IServiceRegistrar AddPerThread<TService>(
        this IServiceRegistrar registrar)
        where TService : class
    {
        return registrar.AddPerThread<TService, TService>();
    }

    /// <summary>
    /// Registers a service with per-thread lifetime using the specified <see cref="PerThreadLifetimeScope"/>.
    /// Use this overload to share a <see cref="PerThreadLifetimeScope"/> across multiple registrations.
    /// </summary>
    /// <typeparam name="TService">The service type (interface or base class)</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <param name="scope">The per-thread lifetime scope to use</param>
    /// <returns>The registrar for fluent chaining</returns>
    public static IServiceRegistrar AddPerThread<TService, TImplementation>(
        this IServiceRegistrar registrar,
        PerThreadLifetimeScope scope)
        where TService : class
        where TImplementation : class, TService
    {
        return registrar.AddWithLifetime<TService, TImplementation>(scope);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    // PER-GRAPH REGISTRATIONS
    // Shared within a single resolution graph, new instance for each resolve call
    // ═══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registers a service with per-resolution-graph lifetime using a new <see cref="PerGraphLifetimeScope"/>.
    /// Instances are shared within a single resolution graph (a single top-level resolve call)
    /// and new instances are created for separate resolve calls.
    /// </summary>
    /// <typeparam name="TService">The service type (interface or base class)</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <returns>The registrar for fluent chaining</returns>
    /// <example>
    /// <code>
    /// partial void ConfigureServices()
    /// {
    ///     this.AddPerGraph&lt;IService, Service&gt;();
    /// }
    /// </code>
    /// </example>
    public static IServiceRegistrar AddPerGraph<TService, TImplementation>(
        this IServiceRegistrar registrar)
        where TService : class
        where TImplementation : class, TService
    {
        var scope = new PerGraphLifetimeScope();
        return registrar.AddWithLifetime<TService, TImplementation>(scope);
    }

    /// <summary>
    /// Registers a service as itself with per-resolution-graph lifetime.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <returns>The registrar for fluent chaining</returns>
    public static IServiceRegistrar AddPerGraph<TService>(
        this IServiceRegistrar registrar)
        where TService : class
    {
        return registrar.AddPerGraph<TService, TService>();
    }

    /// <summary>
    /// Registers a service with per-resolution-graph lifetime using the specified <see cref="PerGraphLifetimeScope"/>.
    /// Use this overload to share a <see cref="PerGraphLifetimeScope"/> across multiple registrations
    /// so they participate in the same resolution graph.
    /// </summary>
    /// <typeparam name="TService">The service type (interface or base class)</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <param name="scope">The per-graph lifetime scope to use</param>
    /// <returns>The registrar for fluent chaining</returns>
    public static IServiceRegistrar AddPerGraph<TService, TImplementation>(
        this IServiceRegistrar registrar,
        PerGraphLifetimeScope scope)
        where TService : class
        where TImplementation : class, TService
    {
        return registrar.AddWithLifetime<TService, TImplementation>(scope);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    // CUSTOM LIFETIME REGISTRATIONS
    // User-defined lifetime scopes for advanced scenarios
    // ═══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registers a service with a custom user-defined lifetime scope.
    /// The lifetime scope controls caching and creation of service instances.
    /// </summary>
    /// <typeparam name="TService">The service type (interface or base class)</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <param name="lifetimeScope">The custom lifetime scope that manages instance creation and caching</param>
    /// <returns>The registrar for fluent chaining</returns>
    /// <example>
    /// <code>
    /// partial void ConfigureServices()
    /// {
    ///     var myScope = new MyCustomLifetimeScope();
    ///     this.AddWithLifetime&lt;IService, Service&gt;(myScope);
    /// }
    /// </code>
    /// </example>
    public static IServiceRegistrar AddWithLifetime<TService, TImplementation>(
        this IServiceRegistrar registrar,
        ILifetimeScope lifetimeScope)
        where TService : class
        where TImplementation : class, TService
    {
        var innerFactory = ServiceFactory<TImplementation>.Create;

        registrar.Register(new ServiceDescriptor
        {
            ServiceType = typeof(TService),
            ImplementationType = typeof(TImplementation),
            Lifetime = Lifetime.Transient,
            Factory = (scope, type, key) => lifetimeScope.GetOrCreate(
                typeof(TService),
                key,
                () => innerFactory(scope, type, key))
        });

        return registrar;
    }

    /// <summary>
    /// Registers a service as itself with a custom user-defined lifetime scope.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <param name="lifetimeScope">The custom lifetime scope that manages instance creation and caching</param>
    /// <returns>The registrar for fluent chaining</returns>
    public static IServiceRegistrar AddWithLifetime<TService>(
        this IServiceRegistrar registrar,
        ILifetimeScope lifetimeScope)
        where TService : class
    {
        return registrar.AddWithLifetime<TService, TService>(lifetimeScope);
    }

    /// <summary>
    /// Registers a service with a custom lifetime scope using a factory delegate.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <param name="lifetimeScope">The custom lifetime scope that manages instance creation and caching</param>
    /// <param name="factory">Factory function that creates the service instance</param>
    /// <returns>The registrar for fluent chaining</returns>
    public static IServiceRegistrar AddWithLifetime<TService>(
        this IServiceRegistrar registrar,
        ILifetimeScope lifetimeScope,
        Func<IServiceScope, TService> factory)
        where TService : class
    {
        registrar.Register(new ServiceDescriptor
        {
            ServiceType = typeof(TService),
            ImplementationType = typeof(TService),
            Lifetime = Lifetime.Transient,
            Factory = (scope, type, key) => lifetimeScope.GetOrCreate(
                typeof(TService),
                key,
                () => factory(scope)!)
        });

        return registrar;
    }
}
