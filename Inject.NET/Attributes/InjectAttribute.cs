namespace Inject.NET.Attributes;

/// <summary>
/// Marks a method or property for injection. Methods marked with this attribute will be called
/// after the service instance is constructed, with their parameters resolved from the container.
/// Properties marked with this attribute will be set after construction with services resolved from the container.
/// </summary>
/// <remarks>
/// <para>
/// Multiple methods and properties can be marked with [Inject] on a single class. Methods are called in declaration order.
/// </para>
/// <para>
/// Async methods (returning Task or ValueTask) are supported and will be awaited during service creation.
/// </para>
/// <para>
/// Nullable properties are treated as optional and will be set to null if the service is not registered.
/// Non-nullable properties are required and will throw if the service is not registered.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyService : IMyService
/// {
///     [Inject]
///     public ILogger Logger { get; set; }
///
///     [Inject]
///     public ICache? OptionalCache { get; set; } // nullable = optional
///
///     [Inject]
///     public void Initialize(ILogger logger, ICache cache)
///     {
///         // called after construction with resolved dependencies
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public sealed class InjectAttribute : Attribute;
