using Inject.NET.Enums;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using Inject.NET.Services;

namespace Inject.NET.Extensions;

/// <summary>
/// Extension methods for registering interceptors with the dependency injection container.
/// Interceptors provide AOP (Aspect-Oriented Programming) capabilities by wrapping service
/// method calls with cross-cutting concerns like logging, caching, retry logic, or authorization.
/// </summary>
/// <remarks>
/// Interceptors are applied using <see cref="System.Reflection.DispatchProxy"/> and require
/// that the service type is an interface. The interceptor wraps the resolved service in a
/// proxy that routes all method calls through the interceptor chain.
/// </remarks>
public static class ServiceRegistrarInterceptorExtensions
{
    // ═══════════════════════════════════════════════════════════════════════════════════
    // INTERCEPTOR REGISTRATIONS
    // Interceptors wrap service calls with cross-cutting concerns via DispatchProxy
    // ═══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Adds an interceptor for a specific service type. The interceptor is registered as a singleton
    /// and will wrap all resolved instances of the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service interface type to intercept. Must be an interface.</typeparam>
    /// <typeparam name="TInterceptor">The interceptor implementation type.</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <returns>The registrar for fluent chaining</returns>
    /// <example>
    /// <code>
    /// partial void ConfigureServices()
    /// {
    ///     this.AddSingleton&lt;IMyService, MyService&gt;()
    ///         .AddInterceptor&lt;IMyService, LoggingInterceptor&gt;();
    /// }
    /// </code>
    /// </example>
    public static IServiceRegistrar AddInterceptor<TService, TInterceptor>(
        this IServiceRegistrar registrar)
        where TService : class
        where TInterceptor : class, IInterceptor
    {
        // Register the interceptor itself as a singleton
        registrar.Register(new ServiceDescriptor
        {
            ServiceType = typeof(TInterceptor),
            ImplementationType = typeof(TInterceptor),
            Lifetime = Lifetime.Singleton,
            Factory = ServiceFactory<TInterceptor>.Create
        });

        // Register the interceptor binding
        registrar.Register(new ServiceDescriptor
        {
            ServiceType = typeof(InterceptorBinding),
            ImplementationType = typeof(InterceptorBinding),
            Lifetime = Lifetime.Singleton,
            Factory = (scope, type, key) => new InterceptorBinding(typeof(TService), typeof(TInterceptor))
        });

        return registrar;
    }

    /// <summary>
    /// Adds a pre-existing interceptor instance for a specific service type.
    /// The instance is externally owned and will NOT be disposed by the container.
    /// </summary>
    /// <typeparam name="TService">The service interface type to intercept. Must be an interface.</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <param name="interceptor">The interceptor instance to use</param>
    /// <returns>The registrar for fluent chaining</returns>
    /// <example>
    /// <code>
    /// partial void ConfigureServices()
    /// {
    ///     var loggingInterceptor = new LoggingInterceptor(myLogger);
    ///     this.AddSingleton&lt;IMyService, MyService&gt;()
    ///         .AddInterceptor&lt;IMyService&gt;(loggingInterceptor);
    /// }
    /// </code>
    /// </example>
    public static IServiceRegistrar AddInterceptor<TService>(
        this IServiceRegistrar registrar,
        IInterceptor interceptor)
        where TService : class
    {
        var interceptorType = interceptor.GetType();

        // Register the interceptor instance
        registrar.Register(new ServiceDescriptor
        {
            ServiceType = interceptorType,
            ImplementationType = interceptorType,
            Lifetime = Lifetime.Singleton,
            ExternallyOwned = true,
            Factory = (scope, type, key) => interceptor
        });

        // Register the interceptor binding
        registrar.Register(new ServiceDescriptor
        {
            ServiceType = typeof(InterceptorBinding),
            ImplementationType = typeof(InterceptorBinding),
            Lifetime = Lifetime.Singleton,
            Factory = (scope, type, key) => new InterceptorBinding(typeof(TService), interceptorType)
        });

        return registrar;
    }

    /// <summary>
    /// Adds an interceptor for a specific service type using a factory function.
    /// </summary>
    /// <typeparam name="TService">The service interface type to intercept. Must be an interface.</typeparam>
    /// <param name="registrar">The service registrar</param>
    /// <param name="interceptorFactory">A factory function that creates the interceptor</param>
    /// <returns>The registrar for fluent chaining</returns>
    public static IServiceRegistrar AddInterceptor<TService>(
        this IServiceRegistrar registrar,
        Func<IServiceScope, IInterceptor> interceptorFactory)
        where TService : class
    {
        // Register the interceptor factory binding directly
        registrar.Register(new ServiceDescriptor
        {
            ServiceType = typeof(InterceptorBinding),
            ImplementationType = typeof(InterceptorBinding),
            Lifetime = Lifetime.Singleton,
            Factory = (scope, type, key) => new InterceptorBinding(typeof(TService), interceptorFactory)
        });

        return registrar;
    }
}
