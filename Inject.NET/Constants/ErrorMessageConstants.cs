namespace Inject.NET.Constants;

/// <summary>
/// Contains constants for commonly used error message patterns.
/// </summary>
public static class ErrorMessageConstants
{
    /// <summary>
    /// Error message when no singleton is registered for a type.
    /// </summary>
    public const string NoSingletonRegistered = "No singleton registered for type {0}.";

    /// <summary>
    /// Error message when no services are registered for a type.
    /// </summary>
    public const string NoServicesRegistered = "No services registered for type {0}.";

    /// <summary>
    /// Error message when no singleton is registered for a type with a specific key.
    /// </summary>
    public const string NoSingletonRegisteredWithKey = "No singleton registered for type {0} with key '{1}'.";

    /// <summary>
    /// Error message when no services are registered for a type with a specific key.
    /// </summary>
    public const string NoServicesRegisteredWithKey = "No services registered for type {0} with key '{1}'.";
}