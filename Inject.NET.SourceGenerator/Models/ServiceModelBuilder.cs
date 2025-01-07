using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Models;

public class ServiceModelBuilder
{
    public required INamedTypeSymbol ServiceType { get; init; }
    
    public required INamedTypeSymbol ImplementationType { get; init; }
    
    public required bool IsOpenGeneric { get; init; }
    
    public required Lifetime Lifetime { get; init; }
    
    public required string? Key { get; init; }

    public required Parameter[] Parameters { get; init; }
}