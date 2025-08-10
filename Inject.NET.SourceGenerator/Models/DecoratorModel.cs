using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Models;

public record DecoratorModel
{
    public required INamedTypeSymbol ServiceType { get; init; }
    
    public required INamedTypeSymbol DecoratorType { get; init; }
    
    public required Lifetime Lifetime { get; init; }
    
    public required int Order { get; init; }
    
    public required Parameter[] Parameters { get; init; }
    
    public required string? Key { get; init; }
    
    public required string? TenantName { get; init; }
    
    public ServiceModelCollection.ServiceKey ServiceKey => new(ServiceType, Key);
}