namespace Inject.NET.Attributes;

/// <summary>
/// Registers a scoped decorator that wraps an existing service implementation.
/// A new decorator instance is created once per scope.
/// </summary>
/// <example>
/// <code>
/// [ServiceProvider]
/// [Scoped&lt;IRepository, SqlRepository&gt;]
/// [ScopedDecorator&lt;IRepository, TransactionRepositoryDecorator&gt;]
/// public partial class ServiceContainer;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ScopedDecoratorAttribute : DecoratorAttribute
{
    /// <summary>
    /// Initializes a new instance of the ScopedDecoratorAttribute class.
    /// </summary>
    /// <param name="serviceType">The service type to decorate</param>
    /// <param name="decoratorType">The decorator implementation type</param>
    public ScopedDecoratorAttribute(Type serviceType, Type decoratorType) : base(serviceType, decoratorType)
    {
    }
}

/// <summary>
/// Registers a scoped decorator with compile-time type safety that wraps an existing service implementation.
/// A new decorator instance is created once per scope.
/// </summary>
/// <typeparam name="TService">The service type interface or base class to decorate</typeparam>
/// <typeparam name="TDecorator">The decorator implementation type</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ScopedDecoratorAttribute<TService, TDecorator> : DecoratorAttribute
    where TService : class
    where TDecorator : class, TService
{
    /// <summary>
    /// Initializes a new instance of the ScopedDecoratorAttribute class.
    /// </summary>
    public ScopedDecoratorAttribute() : base(typeof(TService), typeof(TDecorator))
    {
    }
}