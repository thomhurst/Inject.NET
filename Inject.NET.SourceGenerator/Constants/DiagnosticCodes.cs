namespace Inject.NET.SourceGenerator.Constants;

/// <summary>
/// Contains diagnostic codes used throughout the source generator.
/// </summary>
public static class DiagnosticCodes
{
    /// <summary>
    /// Diagnostic code for circular dependency conflicts.
    /// </summary>
    public const string CircularDependency = "IJN0001";

    /// <summary>
    /// Diagnostic code for factory method not found on the specified type.
    /// </summary>
    public const string FactoryMethodNotFound = "IJN0002";
}