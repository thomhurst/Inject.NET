using Inject.NET.Enums;
using Inject.NET.Interfaces;

namespace Inject.NET.Models;

public class ServiceDescriptor
{
    public required Type Type { get; init; }
    public required Lifetime Lifetime { get; init; }
    public required Func<IServiceScope, Type, object> Factory { get; init; }
}