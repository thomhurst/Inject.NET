using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Models;

public record CompositeModel
{
    public required INamedTypeSymbol ServiceType { get; init; }

    public required INamedTypeSymbol CompositeType { get; init; }

    public required Parameter[] Parameters { get; init; }

    public required string? Key { get; init; }

    public required string? TenantName { get; init; }

    public ServiceModelCollection.ServiceKey ServiceKey => new(ServiceType, Key);
}
