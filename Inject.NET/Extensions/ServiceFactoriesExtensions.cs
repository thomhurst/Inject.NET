using Inject.NET.Enums;
using Inject.NET.Models;

namespace Inject.NET.Extensions;

public static class ServiceFactoriesExtensions
{
    public static ServiceFactories AsReadOnly(this ServiceFactoryBuilders factoryBuilders)
    {
        return new ServiceFactories
        {
            Factories = factoryBuilders.Descriptors.ToFrozenDictionary(),
            KeyedFactories = factoryBuilders.KeyedDescriptors.ToFrozenDictionary(),
        };
    }
}