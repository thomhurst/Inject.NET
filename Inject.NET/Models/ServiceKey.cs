namespace Inject.NET.Models;

public readonly record struct ServiceKey(Type Type, string? Key = null)
{
    public static implicit operator ServiceKey(Type type) => new(type);
}