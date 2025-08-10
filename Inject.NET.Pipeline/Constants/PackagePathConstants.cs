namespace Inject.NET.Pipeline.Constants;

/// <summary>
/// Contains constants used for parsing package paths from build output.
/// </summary>
public static class PackagePathConstants
{
    /// <summary>
    /// Array index for accessing the second part after splitting on the package creation message.
    /// </summary>
    public const int PackagePathAfterMessageIndex = 1;

    /// <summary>
    /// Array index for accessing the first part when splitting on the closing quote and period.
    /// </summary>
    public const int PackagePathBeforeEndIndex = 0;

    /// <summary>
    /// The message prefix that appears when a NuGet package is successfully created.
    /// </summary>
    public const string SuccessfullyCreatedPackageMessage = "Successfully created package '";

    /// <summary>
    /// The message suffix that appears after the package path in the success message.
    /// </summary>
    public const string PackagePathEndDelimiter = "'.";
}