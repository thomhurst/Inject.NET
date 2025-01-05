using System.Collections.Frozen;
using Inject.NET.Enums;
using Inject.NET.Interfaces;

namespace Inject.NET.Models;

public record ServiceFactories
{
    internal ServiceFactories()
    {
    }
    
    public required FrozenDictionary<Type, FrozenSet<(Lifetime, Func<IServiceScope, Type, object>)>> Factories { get; init; }
    public required FrozenDictionary<Type, FrozenDictionary<string, FrozenSet<(Lifetime, Func<IServiceScope, Type, string, object>)>>> KeyedFactories { get; init; }
}