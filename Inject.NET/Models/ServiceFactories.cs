using System.Collections.Frozen;

namespace Inject.NET.Models;

public record ServiceFactories(
    FrozenDictionary<Type, FrozenSet<IServiceDescriptor>> EnumerableDescriptors,
    FrozenDictionary<Type, FrozenDictionary<string, FrozenSet<IKeyedServiceDescriptor>>> KeyedEnumerableDescriptors
    )
{
    public FrozenDictionary<Type, IServiceDescriptor> Descriptor { get; } =
        EnumerableDescriptors.ToFrozenDictionary(
            x => x.Key,
            x => x.Value.Last()
        );

    public FrozenDictionary<Type, FrozenDictionary<string, IKeyedServiceDescriptor>> KeyedDescriptor { get; } =
        KeyedEnumerableDescriptors.ToFrozenDictionary(
            x => x.Key, x => x.Value.ToFrozenDictionary(
                y => y.Key,
                y => y.Value.Last()
            )
        );

}