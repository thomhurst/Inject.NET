using System.Collections.Frozen;
using Inject.NET.Models;

namespace Inject.NET.Extensions;

public static class ServiceFactoriesExtensions
{
    public static ServiceFactories AsReadOnly(this ServiceFactoryBuilders factoryBuilders)
    {
        return new ServiceFactories
        (
            Descriptors: factoryBuilders.Descriptors.GroupBy(x => new ServiceKey(x.ServiceType, x.Key))
                .ToFrozenDictionary(
                    x => x.Key,
                    x => x.ToFrozenSet()
                )
        );
    }
}