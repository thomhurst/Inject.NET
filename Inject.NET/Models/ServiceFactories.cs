using System.Collections.Frozen;

namespace Inject.NET.Models;

public record ServiceFactories(
    FrozenDictionary<ServiceKey, FrozenSet<ServiceDescriptor>> Descriptors
    )
{
    public FrozenDictionary<ServiceKey, ServiceDescriptor> Descriptor { get; } =
        Descriptors.ToFrozenDictionary(
            x => x.Key,
            x => x.Value.Last()
        );

}