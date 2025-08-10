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
        return obj switch
        {
            null => false,
            _ when ReferenceEquals(this, obj) => true,
            ServiceCollection other => Equals(other),
            _ => false
        };
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
    public required object? DefaultValue { get; init; }
    public required bool IsOptional { get; init; }
    public required bool IsNullable { get; init; }
    public required bool IsEnumerable { get; init; }
    public string? Key { get; init; }
    
    public ServiceModelCollection.ServiceKey ServiceKey => new(Type, Key);

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