namespace Inject.NET.Attributes;

/// <summary>
/// Marks a class as a service provider container for dependency injection.
/// Classes marked with this attribute will have a source-generated service provider implementation created at compile time.
/// </summary>
/// <example>
/// <code>
/// [ServiceProvider]
/// [Singleton&lt;IMyService, MyService&gt;]
/// public partial class AppServiceProvider;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ServiceProviderAttribute : Attribute;