using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Models;

public record ServiceModel
{
    public required INamedTypeSymbol ServiceType { get; init; }
    
    public required INamedTypeSymbol ImplementationType { get; init; }
    
    public required Lifetime Lifetime { get; init; }
    
    public required bool IsOpenGeneric { get; init; }
    
    public required string? Key { get; init; }
    
    public required bool ResolvedFromParent { get; init; }
    
    public required Parameter[] Parameters { get; init; }
    
    public required int Index { get; init; }
    
    public ServiceModelCollection.ServiceKey ServiceKey => new(ServiceType, Key);
    public required string? TenantName { get; init; }

    public IEnumerable<ServiceModel> GetAllNestedParameters(IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencyDictionary)
    {
        foreach (var serviceModel in Parameters
                     .Where(x => dependencyDictionary.Keys.Select(k => k.Type).Contains(x.Type, SymbolEqualityComparer.Default))
                     .SelectMany(x => dependencyDictionary[new ServiceModelCollection.ServiceKey(x.Type, x.Key)]))
        {
            yield return serviceModel;

            foreach (var nestedParameter in serviceModel.GetAllNestedParameters(dependencyDictionary))
            {
                yield return nestedParameter;
            }
        }
    }

    public string GetNewServiceKeyInvocation()
    {
        var key = Key is null ? "null" : $"\"{Key}\"";
        return $$"""new ServiceKey { Type = typeof({{ServiceType.GloballyQualified()}}), Key = {{key}} }""";
    }
    
    public string GetPropertyName()
    {
        return NameHelper.AsProperty(this);
    }
}