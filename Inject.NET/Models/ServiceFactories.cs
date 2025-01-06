using System.Collections.Frozen;

namespace Inject.NET.Models;

public record ServiceFactories(
    FrozenDictionary<ServiceKey, FrozenSet<IServiceDescriptor>> Descriptors
    )
{
    public FrozenDictionary<ServiceKey, IServiceDescriptor> Descriptor { get; } =
        Descriptors.ToFrozenDictionary(
            x => x.Key,
            x => x.Value.Last()
        );

}