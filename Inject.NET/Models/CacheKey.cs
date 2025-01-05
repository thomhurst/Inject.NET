namespace Inject.NET.Models;

public readonly record struct CacheKey(Type Type, string? Key = null)
{
}