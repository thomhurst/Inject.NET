namespace Inject.NET.Attributes;

/// <summary>
/// Registers a transient decorator that wraps an existing service implementation.
/// A new decorator instance is created for each request.
/// </summary>
/// <example>
/// <code>
/// [ServiceProvider]
/// [Transient&lt;ICommand, SaveCommand&gt;]
/// [TransientDecorator&lt;ICommand, LoggingCommandDecorator&gt;]
/// [TransientDecorator&lt;ICommand, ValidationCommandDecorator&gt;]
/// public partial class ServiceContainer;
/// // Results in: ValidationCommandDecorator(LoggingCommandDecorator(SaveCommand))
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class TransientDecoratorAttribute : DecoratorAttribute
{
    /// <summary>
    /// Initializes a new instance of the TransientDecoratorAttribute class.
    /// </summary>
    /// <param name="serviceType">The service type to decorate</param>
    /// <param name="decoratorType">The decorator implementation type</param>
    public TransientDecoratorAttribute(Type serviceType, Type decoratorType) : base(serviceType, decoratorType)
    {
    }
}

/// <summary>
/// Registers a transient decorator with compile-time type safety that wraps an existing service implementation.
/// A new decorator instance is created for each request.
/// </summary>
/// <typeparam name="TService">The service type interface or base class to decorate</typeparam>
/// <typeparam name="TDecorator">The decorator implementation type</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class TransientDecoratorAttribute<TService, TDecorator> : DecoratorAttribute
    where TService : class
    where TDecorator : class, TService
{
    /// <summary>
    /// Initializes a new instance of the TransientDecoratorAttribute class.
    /// </summary>
    public TransientDecoratorAttribute() : base(typeof(TService), typeof(TDecorator))
    {
    }
}