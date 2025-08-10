using Inject.NET.Enums;

namespace Inject.NET.Attributes;

/// <summary>
/// Registers a type as a transient service with the dependency injection container.
/// Transient services are created every time they are requested.
/// </summary>
/// <example>
/// <code>
/// [ServiceProvider]
/// [Transient(typeof(MyService))] // Register MyService as itself
/// [Transient(typeof(IMyService), typeof(MyService))] // Register MyService as IMyService
/// public partial class ServiceContainer;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class TransientAttribute : DependencyInjectionAttribute 
{
    /// <summary>
    /// Initializes a new instance of the TransientAttribute class.
    /// Registers the implementation type as both service and implementation.
    /// </summary>
    /// <param name="implementationType">The type to register as transient</param>
    public TransientAttribute(Type implementationType) : base(implementationType, implementationType)
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the TransientAttribute class.
    /// Registers the implementation type as the specified service type.
    /// </summary>
    /// <param name="serviceType">The service type interface or base class</param>
    /// <param name="implementationType">The concrete implementation type</param>
    public TransientAttribute(Type serviceType, Type implementationType) : base(serviceType, implementationType)
    {
    }

    /// <summary>
    /// Gets the lifetime of the service, which is always Transient.
    /// </summary>
    public override Lifetime Lifetime => Lifetime.Transient;
}

/// <summary>
/// Registers a type as a transient service with compile-time type safety.
/// Transient services are created every time they are requested.
/// </summary>
/// <typeparam name="TImplementation">The implementation type to register as transient</typeparam>
/// <example>
/// <code>
/// [ServiceProvider]
/// [Transient&lt;MyService&gt;] // Register MyService as itself
/// public partial class ServiceContainer;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class TransientAttribute<TImplementation> : DependencyInjectionAttribute<TImplementation, TImplementation> 
    where TImplementation : class
{
    /// <summary>
    /// Gets the lifetime of the service, which is always Transient.
    /// </summary>
    public override Lifetime Lifetime => Lifetime.Transient;
}

/// <summary>
/// Registers an implementation type as a transient service for a specific service type with compile-time type safety.
/// Transient services are created every time they are requested.
/// </summary>
/// <typeparam name="TService">The service type interface or base class</typeparam>
/// <typeparam name="TImplementation">The concrete implementation type</typeparam>
/// <example>
/// <code>
/// [ServiceProvider]
/// [Transient&lt;IMyService, MyService&gt;] // Register MyService as IMyService
/// public partial class ServiceContainer;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class TransientAttribute<TService, TImplementation> : DependencyInjectionAttribute<TService, TImplementation> 
    where TService : class 
    where TImplementation : class, TService
{
    /// <summary>
    /// Gets the lifetime of the service, which is always Transient.
    /// </summary>
    public override Lifetime Lifetime => Lifetime.Transient;
}