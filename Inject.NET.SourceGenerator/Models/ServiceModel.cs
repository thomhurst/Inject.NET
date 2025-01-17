using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Models;

public record ServiceModel
{
    public required INamedTypeSymbol ServiceType { get; init; }
    
    public required INamedTypeSymbol ImplementationType { get; init; }
    
    public required Lifetime Lifetime { get; init; }
    
    public required bool IsOpenGeneric { get; init; }
    
    public required string? Key { get; init; }
    
    public bool ResolvedFromParent { get; set; }
    
    public required Parameter[] Parameters { get; init; }

    public IEnumerable<ServiceModel> GetAllNestedParameters(IDictionary<ISymbol?, List<ServiceModel>> dependencyDictionary)
    {
        foreach (var serviceModel in Parameters
                     .Where(x => dependencyDictionary.Keys.Contains(x.Type, SymbolEqualityComparer.Default))
                     .SelectMany(x => dependencyDictionary[x.Type]))
        {
            yield return serviceModel;

            foreach (var nestedParameter in serviceModel.GetAllNestedParameters(dependencyDictionary))
            {
                yield return nestedParameter;
            }
        }
    }

    public string GetKey()
    {
        var key = Key is null ? "null" : $"\"{Key}\"";
        return $$"""new ServiceKey { Type = typeof({{ServiceType.GloballyQualified()}}), Key = {{key}} }""";
    }
}