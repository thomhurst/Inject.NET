using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Models;

/// <summary>
/// Represents a property marked with [Inject] on a service implementation type.
/// The property will be set after construction with a service resolved from the container.
/// </summary>
public record InjectProperty
{
    /// <summary>
    /// The name of the property to set.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// The type of the property.
    /// </summary>
    public required ITypeSymbol PropertyType { get; init; }

    /// <summary>
    /// Whether the property type is nullable (optional).
    /// Nullable properties use GetOptionalService; non-nullable use GetRequiredService.
    /// </summary>
    public required bool IsNullable { get; init; }

    /// <summary>
    /// Whether the property type is Lazy&lt;T&gt;.
    /// </summary>
    public bool IsLazy { get; init; }

    /// <summary>
    /// The inner type of Lazy&lt;T&gt; if IsLazy is true.
    /// </summary>
    public ITypeSymbol? LazyInnerType { get; init; }

    /// <summary>
    /// Whether the property type is Func&lt;T&gt;.
    /// </summary>
    public bool IsFunc { get; init; }

    /// <summary>
    /// The inner type of Func&lt;T&gt; if IsFunc is true.
    /// </summary>
    public ITypeSymbol? FuncInnerType { get; init; }

    /// <summary>
    /// Whether the property type is an enumerable (IEnumerable&lt;T&gt; or similar).
    /// </summary>
    public bool IsEnumerable { get; init; }

    /// <summary>
    /// The optional service key from [ServiceKey] attribute, if present.
    /// </summary>
    public string? Key { get; init; }
}
