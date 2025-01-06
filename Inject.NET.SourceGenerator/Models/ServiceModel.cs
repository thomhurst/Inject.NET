using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Models;

public class ServiceModel
{
    public required INamedTypeSymbol ServiceType { get; init; }
    
    public required INamedTypeSymbol ImplementationType { get; init; }
    
    public required Lifetime Lifetime { get; init; }
    
    public required string? Key { get; init; }
    

    public required ServiceModelParameter[] Parameters { get; init; }
}

public record ServiceModelParameter : Parameter
{
    public List<ServiceModel> ServiceModels { get; } = [];
}