namespace Inject.NET.SourceGenerator.Models;

/// <summary>
/// Represents a method marked with [Inject] on a service implementation type.
/// The method will be called after construction with its parameters resolved from the container.
/// </summary>
public record InjectMethod
{
    /// <summary>
    /// The name of the method to call.
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    /// The parameters of the method that need to be resolved from the container.
    /// </summary>
    public required Parameter[] Parameters { get; init; }

    /// <summary>
    /// Whether the method returns Task (is async).
    /// </summary>
    public required bool ReturnsTask { get; init; }

    /// <summary>
    /// Whether the method returns ValueTask (is async).
    /// </summary>
    public required bool ReturnsValueTask { get; init; }

    /// <summary>
    /// Whether the method is async (returns Task or ValueTask).
    /// </summary>
    public bool IsAsync => ReturnsTask || ReturnsValueTask;
}
