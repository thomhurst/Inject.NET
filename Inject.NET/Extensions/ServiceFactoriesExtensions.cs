using System.Collections.Frozen;
using Inject.NET.Models;

namespace Inject.NET.Extensions;

public static class ServiceFactoriesExtensions
{
    public static ServiceFactories AsReadOnly(this ServiceFactoryBuilders factoryBuilders)
    {
        return new ServiceFactories
        (
            EnumerableDescriptors: factoryBuilders.Descriptors.GroupBy(x => x.ServiceType)
                .ToFrozenDictionary(x => x.Key, x => x.ToFrozenSet()),
            
            KeyedEnumerableDescriptors: factoryBuilders.KeyedDescriptors.GroupBy(x => x.ServiceType)
                .ToFrozenDictionary(x => x.Key, x => x.GroupBy(y => y.Key)
                    .ToFrozenDictionary(y => y.Key, y => y.ToFrozenSet()))
        );
    }
}