using System.Collections.Frozen;
using Inject.NET.Enums;
using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Extensions;

internal static class DictionaryExtensions
{
    public static FrozenDictionary<Type, FrozenSet<(Lifetime, Func<IServiceScope, Type, object>)>> ToFrozenDictionary(
        this IEnumerable<ServiceDescriptor> serviceDescriptors)
    {
        return serviceDescriptors
            .GroupBy(x => x.Type)
            .ToFrozenDictionary(
                x => x.Key,
                x => x.Select(sd => (sd.Lifetime, sd.Factory)).ToFrozenSet()
            );
    }

    public static FrozenDictionary<Type, FrozenDictionary<string, FrozenSet<(Lifetime, Func<IServiceScope, Type, string, object>)>>> ToFrozenDictionary(
        this IEnumerable<KeyedServiceDescriptor> keyedServiceDescriptors)
    {
        return keyedServiceDescriptors
            .GroupBy(x => x.Type)
            .ToFrozenDictionary(
            x => x.Key,
            x => x.GroupBy(y => y.Key).ToFrozenDictionary(
                y => y.Key,
                y => y.Select(z => (z.Lifetime, z.Factory)).ToFrozenSet()
            )
        );
    }
}