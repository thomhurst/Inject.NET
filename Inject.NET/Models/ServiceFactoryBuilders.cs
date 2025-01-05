using Inject.NET.Enums;
using Inject.NET.Interfaces;

namespace Inject.NET.Models;

public record ServiceFactoryBuilders
{
    public List<ServiceDescriptor> Descriptors { get; } = [];
    public List<KeyedServiceDescriptor> KeyedDescriptors { get; } = [];

    public void Add<T>(Type type, Lifetime lifetime, Func<IServiceScope, Type, T> factory)
    {
        Descriptors.Add(new ServiceDescriptor
        {
            Type = type,
            Lifetime = lifetime,
            Factory = (ss, t) => factory(ss, t)!
        });
    }

    public void Add<T>(Type type, Lifetime lifetime, string key, Func<IServiceScope, Type, string, T> factory)
    {
        KeyedDescriptors.Add(new KeyedServiceDescriptor
        {
            Type = type,
            Key = key,
            Lifetime = lifetime,
            Factory = (ss, t, k) => factory(ss, t, k)!
        });
    }
}