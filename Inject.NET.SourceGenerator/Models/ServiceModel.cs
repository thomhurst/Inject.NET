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

    public required bool ExternallyOwned { get; init; }
    
    // Cached property/field names for performance
    private string? _cachedPropertyName;
    private string? _cachedFieldName;

    public IEnumerable<ServiceModel> GetAllNestedParameters(IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencyDictionary)
    {
        foreach (var serviceModel in Parameters
                     .Where(x => !x.IsFunc) // Func<T> defers resolution, so skip it for nested parameter analysis
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
        return _cachedPropertyName ??= NameHelper.AsProperty(this);
    }
    
    public string GetFieldName()
    {
        return _cachedFieldName ??= NameHelper.AsField(this);
    }
}