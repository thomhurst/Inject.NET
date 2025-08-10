using Inject.NET.Enums;

namespace Inject.NET.Attributes;

/// <summary>
/// Registers a type as a singleton service with the dependency injection container.
/// Singleton services are created once per container and reused for all subsequent requests.
/// </summary>
/// <example>
/// <code>
/// [ServiceProvider]
/// [Singleton(typeof(MyService))] // Register MyService as itself
/// [Singleton(typeof(IMyService), typeof(MyService))] // Register MyService as IMyService
/// public partial class ServiceContainer;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SingletonAttribute : DependencyInjectionAttribute 
{
    /// <summary>
    /// Initializes a new instance of the SingletonAttribute class.
    /// Registers the implementation type as both service and implementation.
    /// </summary>
    /// <param name="implementationType">The type to register as singleton</param>
    public SingletonAttribute(Type implementationType) : base(implementationType, implementationType)
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the SingletonAttribute class.
    /// Registers the implementation type as the specified service type.
    /// </summary>
    /// <param name="serviceType">The service type interface or base class</param>
    /// <param name="implementationType">The concrete implementation type</param>
    public SingletonAttribute(Type serviceType, Type implementationType) : base(serviceType, implementationType)
    {
    }

    /// <summary>
    /// Gets the lifetime of the service, which is always Singleton.
    /// </summary>
    public override Lifetime Lifetime => Lifetime.Singleton;
}

/// <summary>
/// Registers a type as a singleton service with compile-time type safety.
/// Singleton services are created once per container and reused for all subsequent requests.
/// </summary>
/// <typeparam name="TImplementation">The implementation type to register as singleton</typeparam>
/// <example>
/// <code>
/// [ServiceProvider]
/// [Singleton&lt;MyService&gt;] // Register MyService as itself
/// public partial class ServiceContainer;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SingletonAttribute<TImplementation> : DependencyInjectionAttribute<TImplementation, TImplementation> 
    where TImplementation : class
{
    /// <summary>
    /// Gets the lifetime of the service, which is always Singleton.
    /// </summary>
    public override Lifetime Lifetime => Lifetime.Singleton;
}

/// <summary>
/// Registers an implementation type as a singleton service for a specific service type with compile-time type safety.
/// Singleton services are created once per container and reused for all subsequent requests.
/// </summary>
/// <typeparam name="TService">The service type interface or base class</typeparam>
/// <typeparam name="TImplementation">The concrete implementation type</typeparam>
/// <example>
/// <code>
/// [ServiceProvider]
/// [Singleton&lt;IMyService, MyService&gt;] // Register MyService as IMyService
/// public partial class ServiceContainer;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SingletonAttribute<TService, TImplementation> : DependencyInjectionAttribute<TService, TImplementation> 
    where TService : class 
    where TImplementation : class, TService
{
    /// <summary>
    /// Gets the lifetime of the service, which is always Singleton.
    /// </summary>
    public override Lifetime Lifetime => Lifetime.Singleton;
}