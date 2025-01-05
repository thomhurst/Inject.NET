using Inject.NET.Enums;

namespace Inject.NET.Models;

public class OpenGenericServiceDescriptor : IServiceDescriptor
{
    public required Type ServiceType { get; init; }
    public required Type ImplementationType { get; init; }
    public required Lifetime Lifetime { get; init; }
}