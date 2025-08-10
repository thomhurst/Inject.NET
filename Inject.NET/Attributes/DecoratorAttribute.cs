namespace Inject.NET.Attributes;

/// <summary>
/// Base class for decorator attributes that wrap existing service implementations.
/// Decorators are applied in the order they are registered.
/// </summary>
public abstract class DecoratorAttribute : Attribute
{
    /// <summary>
    /// Gets the service type to decorate.
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// Gets the decorator implementation type.
    /// </summary>
    public Type DecoratorType { get; }

    /// <summary>
    /// Gets or sets the order in which this decorator is applied.
    /// Lower values are applied first (closer to the original implementation).
    /// Default is 0. When multiple decorators have the same order, they are applied in registration order.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Initializes a new instance of the DecoratorAttribute class.
    /// </summary>
    /// <param name="serviceType">The service type to decorate</param>
    /// <param name="decoratorType">The decorator implementation type</param>
    protected DecoratorAttribute(Type serviceType, Type decoratorType)
    {
        ServiceType = serviceType;
        DecoratorType = decoratorType;
        Order = 0;
    }
}