using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Models;

public class ServiceModel
{
    public required INamedTypeSymbol ServiceType { get; init; }
    
    public required INamedTypeSymbol ImplementationType { get; init; }
    
    public required Lifetime Lifetime { get; init; }
    
    public required bool IsOpenGeneric { get; init; }
    
    public required string? Key { get; init; }
    
    public required ServiceModelParameter[] Parameters { get; init; }

    public IEnumerable<ServiceModel> GetAllNestedParameters()
    {
        foreach (var serviceModel in Parameters.SelectMany(x => x.ServiceModels))
        {
            yield return serviceModel;

            foreach (var nestedParameter in serviceModel.GetAllNestedParameters())
            {
                yield return nestedParameter;
            }
        }
    }
}

public record ServiceModelParameter : Parameter
{
    public List<ServiceModel> ServiceModels { get; } = [];
}