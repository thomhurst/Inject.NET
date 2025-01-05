using System.Collections.Frozen;

namespace Inject.NET.Models;

public record ServiceFactories(
    FrozenDictionary<CacheKey, FrozenSet<IServiceDescriptor>> Descriptors
    )
{
    public FrozenDictionary<CacheKey, IServiceDescriptor> Descriptor { get; } =
        Descriptors.ToFrozenDictionary(
            x => x.Key,
            x => x.Value.Last()
        );

}