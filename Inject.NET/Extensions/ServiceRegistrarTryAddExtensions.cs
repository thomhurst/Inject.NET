using Inject.NET.Enums;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using Inject.NET.Services;

namespace Inject.NET.Extensions;

/// <summary>
/// Extension methods for conditional service registration with dependency injection containers.
/// TryAdd methods only register a service if no registration for the service type already exists,
/// making them ideal for library authors who want to provide default implementations that
/// consumers can override.
/// </summary>
public static class ServiceRegistrarTryAddExtensions
{
    // ═══════════════════════════════════════════════════════════════════════════════════
    // TRY ADD SINGLETON REGISTRATIONS
    // Only registers if no existing registration for the service type exists
    // ═══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registers a singleton service with separate service and implementation types,
    /// but only if no registration for the service type already exists.
    /// </summary>
    /// <typeparam name="TService">The service type (interface or base class)</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <returns>The registrar for fluent chaining</returns>
    /// <example>
    /// <code>
    /// partial void ConfigureServices()
    /// {
    ///     this.TryAddSingleton&lt;ICache, DefaultCache&gt;(); // registers only if ICache not already registered
    /// }
    /// </code>
    /// </example>
    public static IServiceRegistrar TryAddSingleton<TService, TImplementation>(
        this IServiceRegistrar registrar)
        where TService : class
        where TImplementation : class, TService
    {
        if (registrar.ServiceFactoryBuilders.HasService(typeof(TService)))
        {
            return registrar;
        }

        return registrar.AddSingleton<TService, TImplementation>();
    }

    /// <summary>
    /// Registers a singleton service as itself (service type equals implementation type),
    /// but only if no registration for the service type already exists.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <returns>The registrar for fluent chaining</returns>
    public static IServiceRegistrar TryAddSingleton<TService>(
        this IServiceRegistrar registrar)
        where TService : class
    {
        return registrar.TryAddSingleton<TService, TService>();
    }

    /// <summary>
    /// Registers a singleton service using a factory function,
    /// but only if no registration for the service type already exists.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <param name="factory">Factory function that creates the service instance</param>
    /// <returns>The registrar for fluent chaining</returns>
    public static IServiceRegistrar TryAddSingleton<TService>(
        this IServiceRegistrar registrar,
        Func<IServiceScope, TService> factory)
        where TService : class
    {
        if (registrar.ServiceFactoryBuilders.HasService(typeof(TService)))
        {
            return registrar;
        }

        return registrar.AddSingleton(factory);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    // TRY ADD SCOPED REGISTRATIONS
    // Only registers if no existing registration for the service type exists
    // ═══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registers a scoped service with separate service and implementation types,
    /// but only if no registration for the service type already exists.
    /// </summary>
    /// <typeparam name="TService">The service type (interface or base class)</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <returns>The registrar for fluent chaining</returns>
    public static IServiceRegistrar TryAddScoped<TService, TImplementation>(
        this IServiceRegistrar registrar)
        where TService : class
        where TImplementation : class, TService
    {
        if (registrar.ServiceFactoryBuilders.HasService(typeof(TService)))
        {
            return registrar;
        }

        return registrar.AddScoped<TService, TImplementation>();
    }

    /// <summary>
    /// Registers a scoped service as itself (service type equals implementation type),
    /// but only if no registration for the service type already exists.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <returns>The registrar for fluent chaining</returns>
    public static IServiceRegistrar TryAddScoped<TService>(
        this IServiceRegistrar registrar)
        where TService : class
    {
        return registrar.TryAddScoped<TService, TService>();
    }

    /// <summary>
    /// Registers a scoped service using a factory function,
    /// but only if no registration for the service type already exists.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <param name="factory">Factory function that creates the service instance</param>
    /// <returns>The registrar for fluent chaining</returns>
    public static IServiceRegistrar TryAddScoped<TService>(
        this IServiceRegistrar registrar,
        Func<IServiceScope, TService> factory)
        where TService : class
    {
        if (registrar.ServiceFactoryBuilders.HasService(typeof(TService)))
        {
            return registrar;
        }

        return registrar.AddScoped(factory);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    // TRY ADD TRANSIENT REGISTRATIONS
    // Only registers if no existing registration for the service type exists
    // ═══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registers a transient service with separate service and implementation types,
    /// but only if no registration for the service type already exists.
    /// </summary>
    /// <typeparam name="TService">The service type (interface or base class)</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <returns>The registrar for fluent chaining</returns>
    public static IServiceRegistrar TryAddTransient<TService, TImplementation>(
        this IServiceRegistrar registrar)
        where TService : class
        where TImplementation : class, TService
    {
        if (registrar.ServiceFactoryBuilders.HasService(typeof(TService)))
        {
            return registrar;
        }

        return registrar.AddTransient<TService, TImplementation>();
    }

    /// <summary>
    /// Registers a transient service as itself (service type equals implementation type),
    /// but only if no registration for the service type already exists.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <returns>The registrar for fluent chaining</returns>
    public static IServiceRegistrar TryAddTransient<TService>(
        this IServiceRegistrar registrar)
        where TService : class
    {
        return registrar.TryAddTransient<TService, TService>();
    }

    /// <summary>
    /// Registers a transient service using a factory function,
    /// but only if no registration for the service type already exists.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <param name="factory">Factory function that creates the service instance</param>
    /// <returns>The registrar for fluent chaining</returns>
    public static IServiceRegistrar TryAddTransient<TService>(
        this IServiceRegistrar registrar,
        Func<IServiceScope, TService> factory)
        where TService : class
    {
        if (registrar.ServiceFactoryBuilders.HasService(typeof(TService)))
        {
            return registrar;
        }

        return registrar.AddTransient(factory);
    }
}
