using Inject.NET.Enums;
using Inject.NET.Interfaces;

namespace Inject.NET.Models;

public class KeyedServiceDescriptor : IKeyedServiceDescriptor
{
    public required Type ServiceType { get; init; }
    public required Type ImplementationType { get; init; }
    public required string Key { get; init; }
    public required Lifetime Lifetime { get; init; }
    public required Func<IServiceScope, Type, string, object> Factory { get; init; }
}