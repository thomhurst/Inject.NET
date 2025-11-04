using Inject.NET.Enums;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using Inject.NET.Services;

namespace Inject.NET.Extensions;

/// <summary>
/// Extension methods for fluent service registration with dependency injection containers.
/// Provides a flexible, high-performance alternative to attribute-based registration
/// that supports conditional logic, factory delegates, and complex configuration scenarios.
/// </summary>
/// <remarks>
/// These extension methods use cached expression trees for optimal performance,
/// achieving near compile-time speed (~2ns overhead) after first resolution.
/// </remarks>
public static class ServiceRegistrarExtensions
{
    // ═══════════════════════════════════════════════════════════════════════════════════
    // SINGLETON REGISTRATIONS
    // Services created once and reused for the lifetime of the application
    // ═══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registers a singleton service with separate service and implementation types.
    /// The service is created once when first requested and reused for all subsequent requests.
    /// </summary>
    /// <typeparam name="TService">The service type (interface or base class)</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <returns>The registrar for fluent chaining</returns>
    /// <example>
    /// <code>
    /// partial void ConfigureServices()
    /// {
    ///     this.AddSingleton&lt;ICache, MemoryCache&gt;();
    /// }
    /// </code>
    /// </example>
    public static IServiceRegistrar AddSingleton<TService, TImplementation>(
        this IServiceRegistrar registrar)
        where TService : class
        where TImplementation : class, TService
    {
        registrar.Register(new ServiceDescriptor
        {
            ServiceType = typeof(TService),
            ImplementationType = typeof(TImplementation),
            Lifetime = Lifetime.Singleton,
            Factory = ServiceFactory<TImplementation>.Create
        });

        return registrar;
    }

    /// <summary>
    /// Registers a singleton service as itself (service type equals implementation type).
    /// The service is created once when first requested and reused for all subsequent requests.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <returns>The registrar for fluent chaining</returns>
    /// <example>
    /// <code>
    /// partial void ConfigureServices()
    /// {
    ///     this.AddSingleton&lt;MemoryCache&gt;();
    /// }
    /// </code>
    /// </example>
    public static IServiceRegistrar AddSingleton<TService>(
        this IServiceRegistrar registrar)
        where TService : class
    {
        return registrar.AddSingleton<TService, TService>();
    }

    /// <summary>
    /// Registers a singleton service using a factory function.
    /// The factory is called once when the service is first requested.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <param name="factory">Factory function that creates the service instance</param>
    /// <returns>The registrar for fluent chaining</returns>
    /// <example>
    /// <code>
    /// partial void ConfigureServices()
    /// {
    ///     this.AddSingleton&lt;IConfiguration&gt;(scope =>
    ///         new JsonConfiguration("appsettings.json"));
    /// }
    /// </code>
    /// </example>
    public static IServiceRegistrar AddSingleton<TService>(
        this IServiceRegistrar registrar,
        Func<IServiceScope, TService> factory)
        where TService : class
    {
        registrar.Register(new ServiceDescriptor
        {
            ServiceType = typeof(TService),
            ImplementationType = typeof(TService),
            Lifetime = Lifetime.Singleton,
            Factory = (scope, type, key) => factory(scope)!
        });

        return registrar;
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    // SCOPED REGISTRATIONS
    // Services created once per scope (e.g., per HTTP request in web applications)
    // ═══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registers a scoped service with separate service and implementation types.
    /// A new instance is created for each scope and reused within that scope.
    /// </summary>
    /// <typeparam name="TService">The service type (interface or base class)</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <returns>The registrar for fluent chaining</returns>
    /// <example>
    /// <code>
    /// partial void ConfigureServices()
    /// {
    ///     this.AddScoped&lt;IRepository, SqlRepository&gt;();
    /// }
    /// </code>
    /// </example>
    public static IServiceRegistrar AddScoped<TService, TImplementation>(
        this IServiceRegistrar registrar)
        where TService : class
        where TImplementation : class, TService
    {
        registrar.Register(new ServiceDescriptor
        {
            ServiceType = typeof(TService),
            ImplementationType = typeof(TImplementation),
            Lifetime = Lifetime.Scoped,
            Factory = ServiceFactory<TImplementation>.Create
        });

        return registrar;
    }

    /// <summary>
    /// Registers a scoped service as itself (service type equals implementation type).
    /// A new instance is created for each scope and reused within that scope.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <returns>The registrar for fluent chaining</returns>
    /// <example>
    /// <code>
    /// partial void ConfigureServices()
    /// {
    ///     this.AddScoped&lt;SqlRepository&gt;();
    /// }
    /// </code>
    /// </example>
    public static IServiceRegistrar AddScoped<TService>(
        this IServiceRegistrar registrar)
        where TService : class
    {
        return registrar.AddScoped<TService, TService>();
    }

    /// <summary>
    /// Registers a scoped service using a factory function.
    /// The factory is called once per scope when the service is first requested.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <param name="factory">Factory function that creates the service instance</param>
    /// <returns>The registrar for fluent chaining</returns>
    /// <example>
    /// <code>
    /// partial void ConfigureServices()
    /// {
    ///     this.AddScoped&lt;IDbConnection&gt;(scope =>
    ///     {
    ///         var config = scope.GetRequiredService&lt;IConfiguration&gt;();
    ///         return new SqlConnection(config.ConnectionString);
    ///     });
    /// }
    /// </code>
    /// </example>
    public static IServiceRegistrar AddScoped<TService>(
        this IServiceRegistrar registrar,
        Func<IServiceScope, TService> factory)
        where TService : class
    {
        registrar.Register(new ServiceDescriptor
        {
            ServiceType = typeof(TService),
            ImplementationType = typeof(TService),
            Lifetime = Lifetime.Scoped,
            Factory = (scope, type, key) => factory(scope)!
        });

        return registrar;
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    // TRANSIENT REGISTRATIONS
    // New instance created every time the service is requested
    // ═══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registers a transient service with separate service and implementation types.
    /// A new instance is created every time the service is requested.
    /// </summary>
    /// <typeparam name="TService">The service type (interface or base class)</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <returns>The registrar for fluent chaining</returns>
    /// <example>
    /// <code>
    /// partial void ConfigureServices()
    /// {
    ///     this.AddTransient&lt;IEmailService, SmtpEmailService&gt;();
    /// }
    /// </code>
    /// </example>
    public static IServiceRegistrar AddTransient<TService, TImplementation>(
        this IServiceRegistrar registrar)
        where TService : class
        where TImplementation : class, TService
    {
        registrar.Register(new ServiceDescriptor
        {
            ServiceType = typeof(TService),
            ImplementationType = typeof(TImplementation),
            Lifetime = Lifetime.Transient,
            Factory = ServiceFactory<TImplementation>.Create
        });

        return registrar;
    }

    /// <summary>
    /// Registers a transient service as itself (service type equals implementation type).
    /// A new instance is created every time the service is requested.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <returns>The registrar for fluent chaining</returns>
    /// <example>
    /// <code>
    /// partial void ConfigureServices()
    /// {
    ///     this.AddTransient&lt;EmailService&gt;();
    /// }
    /// </code>
    /// </example>
    public static IServiceRegistrar AddTransient<TService>(
        this IServiceRegistrar registrar)
        where TService : class
    {
        return registrar.AddTransient<TService, TService>();
    }

    /// <summary>
    /// Registers a transient service using a factory function.
    /// The factory is called every time the service is requested.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <param name="factory">Factory function that creates the service instance</param>
    /// <returns>The registrar for fluent chaining</returns>
    /// <example>
    /// <code>
    /// partial void ConfigureServices()
    /// {
    ///     this.AddTransient&lt;IOperationId&gt;(scope =>
    ///         new OperationId(Guid.NewGuid()));
    /// }
    /// </code>
    /// </example>
    public static IServiceRegistrar AddTransient<TService>(
        this IServiceRegistrar registrar,
        Func<IServiceScope, TService> factory)
        where TService : class
    {
        registrar.Register(new ServiceDescriptor
        {
            ServiceType = typeof(TService),
            ImplementationType = typeof(TService),
            Lifetime = Lifetime.Transient,
            Factory = (scope, type, key) => factory(scope)!
        });

        return registrar;
    }
}
