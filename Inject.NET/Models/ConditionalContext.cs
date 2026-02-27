namespace Inject.NET.Models;

/// <summary>
/// Provides context information for conditional service registration predicates.
/// Used to determine whether a service registration should be applied based on
/// the resolution context.
/// </summary>
public class ConditionalContext
{
    /// <summary>
    /// Gets the type of the consumer that is requesting the service, if available.
    /// This is null when the service is being resolved directly from the scope
    /// (i.e., not as a dependency of another service).
    /// </summary>
    public Type? ConsumerType { get; init; }

    /// <summary>
    /// Gets the service type being resolved.
    /// </summary>
    public required Type ServiceType { get; init; }

    /// <summary>
    /// Gets the optional service key used for keyed service resolution.
    /// </summary>
    public string? Key { get; init; }
}