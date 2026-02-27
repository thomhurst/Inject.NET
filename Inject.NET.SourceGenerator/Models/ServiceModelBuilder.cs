using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Models;

public record ServiceModelBuilder
{
    public required INamedTypeSymbol ServiceType { get; init; }
    
    public required INamedTypeSymbol ImplementationType { get; init; }
    
    public required bool IsOpenGeneric { get; init; }
    
    public required Lifetime Lifetime { get; init; }
    
    public required string? Key { get; init; }

    public required Parameter[] Parameters { get; init; }
    public required InjectMethod[] InjectMethods { get; init; }
    public required InjectProperty[] InjectProperties { get; init; }
    public required string? TenantName { get; init; }
    public required bool ExternallyOwned { get; init; }
}