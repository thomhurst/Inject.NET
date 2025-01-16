using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

public class ServiceCollection
{
    public required IEnumerable<Service> Services { get; init; }

    protected bool Equals(ServiceCollection other)
    {
        return Services.SequenceEqual(other.Services);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((ServiceCollection)obj);
    }

    public override int GetHashCode()
    {
        return Services.GetHashCode();
    }
}

public class Service
{
    public required string ServiceType { get; init; }
    
    public required string ImplementationType { get; init; }
    public required Lifetime Lifetime { get; init; }
    
    public required Parameter[] Parameters { get; init; }
    public string? Key { get; set; }
}

public record Parameter
{
    public required ITypeSymbol Type { get; init; }
    public required bool IsOptional { get; init; }
    public required bool IsNullable { get; init; }
    public required bool IsEnumerable { get; init; }
    public string? Key { get; init; }

    public string WriteSource()
    {
        var key = Key is null ? string.Empty : $"\"{Key}\"";
        
        if (IsEnumerable)
        {
            return $"[..scope.GetServices<{Type}>({key})]";
        }

        if (IsOptional)
        {
            return $"scope.GetOptionalService<{Type}>({key})";
        }
        
        return $"scope.GetRequiredService<{Type}>({key})";
    }
}

public enum Lifetime
{
    Singleton,
    Scoped,
    Transient
}