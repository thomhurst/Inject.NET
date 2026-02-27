using Inject.NET.Enums;
using Inject.NET.Interfaces;

namespace Inject.NET.Models;

public class ServiceDescriptor
{
    public required Type ServiceType { get; init; }
    public required Type ImplementationType { get; init; }
    public required Lifetime Lifetime { get; init; }
    public string? Key { get; init; }
    public bool IsComposite { get; init; }

    /// <summary>
    /// When true, the container will not dispose this service when the scope or provider is disposed.
    /// </summary>
    public bool ExternallyOwned { get; init; }

    /// <summary>
    /// An optional predicate that determines whether this service registration should be used.
    /// When null, the registration is unconditional and always applies.
    /// When set, the registration is only used if the predicate returns true for the given context.
    /// </summary>
    public Func<ConditionalContext, bool>? Predicate { get; init; }

    public required Func<IServiceScope, Type, string?, object> Factory { get; init; }
}