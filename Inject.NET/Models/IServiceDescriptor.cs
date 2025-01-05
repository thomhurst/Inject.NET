using Inject.NET.Enums;

namespace Inject.NET.Models;

public interface IServiceDescriptor
{
    Type ServiceType { get; }
    Lifetime Lifetime { get; init; }
}