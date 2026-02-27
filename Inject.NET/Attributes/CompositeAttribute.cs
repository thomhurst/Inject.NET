namespace Inject.NET.Attributes;

/// <summary>
/// Registers a composite that wraps all other registrations of a service type.
/// A composite receives all non-composite implementations via IEnumerable&lt;T&gt; constructor injection.
/// When resolving the service type (singular), the composite is returned.
/// When resolving IEnumerable&lt;T&gt;, the composite is excluded.
/// </summary>
/// <example>
/// <code>
/// [ServiceProvider]
/// [Singleton&lt;INotificationSender, EmailSender&gt;]
/// [Singleton&lt;INotificationSender, SmsSender&gt;]
/// [Composite(typeof(INotificationSender), typeof(CompositeNotificationSender))]
/// public partial class ServiceContainer;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CompositeAttribute : Attribute
{
    /// <summary>
    /// Gets the service type to create a composite for.
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// Gets the composite implementation type.
    /// </summary>
    public Type CompositeType { get; }

    /// <summary>
    /// Initializes a new instance of the CompositeAttribute class.
    /// </summary>
    /// <param name="serviceType">The service type to create a composite for</param>
    /// <param name="compositeType">The composite implementation type</param>
    public CompositeAttribute(Type serviceType, Type compositeType)
    {
        ServiceType = serviceType;
        CompositeType = compositeType;
    }
}

/// <summary>
/// Registers a composite with compile-time type safety that wraps all other registrations of a service type.
/// The composite receives all non-composite implementations via IEnumerable&lt;T&gt; constructor injection.
/// When resolving the service type (singular), the composite is returned.
/// When resolving IEnumerable&lt;T&gt;, the composite is excluded.
/// </summary>
/// <typeparam name="TService">The service type interface or base class</typeparam>
/// <typeparam name="TComposite">The composite implementation type</typeparam>
/// <example>
/// <code>
/// [ServiceProvider]
/// [Singleton&lt;INotificationSender, EmailSender&gt;]
/// [Singleton&lt;INotificationSender, SmsSender&gt;]
/// [Composite&lt;INotificationSender, CompositeNotificationSender&gt;]
/// public partial class ServiceContainer;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class CompositeAttribute<TService, TComposite> : CompositeAttribute
    where TService : class
    where TComposite : class, TService
{
    /// <summary>
    /// Initializes a new instance of the CompositeAttribute class.
    /// </summary>
    public CompositeAttribute() : base(typeof(TService), typeof(TComposite))
    {
    }
}
