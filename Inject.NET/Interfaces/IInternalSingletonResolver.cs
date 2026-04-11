using Inject.NET.Models;

namespace Inject.NET.Interfaces;

/// <summary>
/// Internal non-generic contract for resolving singleton instances by <see cref="ServiceKey"/>.
/// Implemented by both the generic <see cref="Services.ServiceProvider{TSelf, TSingletonScope, TScope, TParentServiceProvider, TParentSingletonScope, TParentServiceScope}"/>
/// base and <see cref="Services.ChildServiceProvider"/>, allowing a child container to reuse
/// the parent's already-constructed singleton instances for inherited (non-overridden) keys.
/// </summary>
internal interface IInternalSingletonResolver
{
    /// <summary>
    /// Attempts to resolve singleton instances for the given service key from this provider's
    /// own singleton scope, and transitively from any parent providers.
    /// Returns an empty list when the key has no singleton registrations in this chain.
    /// </summary>
    IReadOnlyList<object> ResolveSingletons(ServiceKey serviceKey);
}
