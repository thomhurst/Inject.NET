namespace Inject.NET.Attributes;

/// <summary>
/// Marks a method for method injection. Methods marked with this attribute will be called
/// after the service instance is constructed, with their parameters resolved from the container.
/// </summary>
/// <remarks>
/// <para>
/// Multiple methods can be marked with [Inject] on a single class. They are called in declaration order.
/// </para>
/// <para>
/// Async methods (returning Task or ValueTask) are supported and will be awaited during service creation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyService : IMyService
/// {
///     [Inject]
///     public void Initialize(ILogger logger, ICache cache)
///     {
///         // called after construction with resolved dependencies
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class InjectAttribute : Attribute;
