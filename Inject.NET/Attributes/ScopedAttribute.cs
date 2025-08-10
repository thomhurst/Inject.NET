using Inject.NET.Enums;

namespace Inject.NET.Attributes;

/// <summary>
/// Registers a type as a scoped service with the dependency injection container.
/// Scoped services are created once per scope and reused within that scope.
/// </summary>
/// <example>
/// <code>
/// [ServiceProvider]
/// [Scoped(typeof(MyService))] // Register MyService as itself
/// [Scoped(typeof(IMyService), typeof(MyService))] // Register MyService as IMyService
/// public partial class ServiceContainer;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ScopedAttribute : DependencyInjectionAttribute 
{
    /// <summary>
    /// Initializes a new instance of the ScopedAttribute class.
    /// Registers the implementation type as both service and implementation.
    /// </summary>
    /// <param name="implementationType">The type to register as scoped</param>
    public ScopedAttribute(Type implementationType) : base(implementationType, implementationType)
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the ScopedAttribute class.
    /// Registers the implementation type as the specified service type.
    /// </summary>
    /// <param name="serviceType">The service type interface or base class</param>
    /// <param name="implementationType">The concrete implementation type</param>
    public ScopedAttribute(Type serviceType, Type implementationType) : base(serviceType, implementationType)
    {
    }

    /// <summary>
    /// Gets the lifetime of the service, which is always Scoped.
    /// </summary>
    public override Lifetime Lifetime => Lifetime.Scoped;
}

/// <summary>
/// Registers a type as a scoped service with compile-time type safety.
/// Scoped services are created once per scope and reused within that scope.
/// </summary>
/// <typeparam name="TImplementation">The implementation type to register as scoped</typeparam>
/// <example>
/// <code>
/// [ServiceProvider]
/// [Scoped&lt;MyService&gt;] // Register MyService as itself
/// public partial class ServiceContainer;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ScopedAttribute<TImplementation> : DependencyInjectionAttribute<TImplementation, TImplementation> 
    where TImplementation : class
{
    /// <summary>
    /// Gets the lifetime of the service, which is always Scoped.
    /// </summary>
    public override Lifetime Lifetime => Lifetime.Scoped;
}

/// <summary>
/// Registers an implementation type as a scoped service for a specific service type with compile-time type safety.
/// Scoped services are created once per scope and reused within that scope.
/// </summary>
/// <typeparam name="TService">The service type interface or base class</typeparam>
/// <typeparam name="TImplementation">The concrete implementation type</typeparam>
/// <example>
/// <code>
/// [ServiceProvider]
/// [Scoped&lt;IMyService, MyService&gt;] // Register MyService as IMyService
/// public partial class ServiceContainer;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ScopedAttribute<TService, TImplementation> : DependencyInjectionAttribute<TService, TImplementation> 
    where TService : class 
    where TImplementation : class, TService
{
    /// <summary>
    /// Gets the lifetime of the service, which is always Scoped.
    /// </summary>
    public override Lifetime Lifetime => Lifetime.Scoped;
}