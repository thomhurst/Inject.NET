namespace Inject.NET.Attributes;

/// <summary>
/// Includes service registrations from a module class into the service provider.
/// The module class should have dependency injection attributes (e.g., [Singleton], [Scoped], [Transient])
/// that define which services to register.
/// Modules can reference other modules via [UseModule], and circular references are detected at compile time.
/// </summary>
/// <typeparam name="TModule">The module class whose registration attributes should be included</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class UseModuleAttribute<TModule> : Attribute where TModule : class;
