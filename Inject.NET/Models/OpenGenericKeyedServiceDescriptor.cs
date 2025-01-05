using Inject.NET.Enums;

namespace Inject.NET.Models;

public class OpenGenericKeyedServiceDescriptor : IKeyedServiceDescriptor
{
    public required Type ServiceType { get; init; }
    public required Type ImplementationType { get; init; }
    public required Lifetime Lifetime { get; init; }
    public required string Key { get; init; }
}