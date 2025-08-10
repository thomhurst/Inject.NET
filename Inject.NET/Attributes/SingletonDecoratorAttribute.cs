namespace Inject.NET.Attributes;

/// <summary>
/// Registers a singleton decorator that wraps an existing service implementation.
/// The decorator instance is created once and reused for all requests.
/// </summary>
/// <example>
/// <code>
/// [ServiceProvider]
/// [Singleton&lt;ILogger, ConsoleLogger&gt;]
/// [SingletonDecorator&lt;ILogger, CachingLoggerDecorator&gt;]
/// public partial class ServiceContainer;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SingletonDecoratorAttribute : DecoratorAttribute
{
    /// <summary>
    /// Initializes a new instance of the SingletonDecoratorAttribute class.
    /// </summary>
    /// <param name="serviceType">The service type to decorate</param>
    /// <param name="decoratorType">The decorator implementation type</param>
    public SingletonDecoratorAttribute(Type serviceType, Type decoratorType) : base(serviceType, decoratorType)
    {
    }
}

/// <summary>
/// Registers a singleton decorator with compile-time type safety that wraps an existing service implementation.
/// The decorator instance is created once and reused for all requests.
/// </summary>
/// <typeparam name="TService">The service type interface or base class to decorate</typeparam>
/// <typeparam name="TDecorator">The decorator implementation type</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SingletonDecoratorAttribute<TService, TDecorator> : DecoratorAttribute
    where TService : class
    where TDecorator : class, TService
{
    /// <summary>
    /// Initializes a new instance of the SingletonDecoratorAttribute class.
    /// </summary>
    public SingletonDecoratorAttribute() : base(typeof(TService), typeof(TDecorator))
    {
    }
}