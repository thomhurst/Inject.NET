using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Helpers;

public class OverriddenDependenciesHelper
{
    public static Dictionary<ISymbol?, ServiceModel[]> GetOverriddenDependencies(
        Dictionary<ISymbol?, ServiceModel[]> dependencies, Dictionary<ISymbol?, ServiceModel[]> parentDependencies)
    {
        return GetOverriddenDependenciesEnumerable(dependencies, parentDependencies)
            .GroupBy(x => x.ServiceType, SymbolEqualityComparer.Default)
            .ToDictionary(x => x.Key, x => x.ToArray(), SymbolEqualityComparer.Default);
    }

    private static IEnumerable<ServiceModel> GetOverriddenDependenciesEnumerable(
        Dictionary<ISymbol?, ServiceModel[]> dependencies, Dictionary<ISymbol?, ServiceModel[]> parentDependencies)
    {
        if (parentDependencies.Count == 0)
        {
            yield break;
        }
        
        var allDependencies = Merge(dependencies, parentDependencies);
        foreach (var (key, serviceModels) in parentDependencies)
        {
            foreach (var serviceModel in serviceModels)
            {
                foreach (var serviceModelDependency in serviceModel.GetAllNestedParameters(allDependencies))
                {
                    if (dependencies.Keys.Contains(serviceModelDependency.ServiceType, SymbolEqualityComparer.Default))
                    {
                        yield return serviceModel;
                    }
                }
            }
        }
    }

    public static Dictionary<ISymbol?, ServiceModel[]> Merge(
        Dictionary<ISymbol?, ServiceModel[]> one, Dictionary<ISymbol?, ServiceModel[]> two)
    {
        return one.Concat(two)
            .GroupBy(x => x.Key, SymbolEqualityComparer.Default)
            .ToDictionary(x => x.Key, x => x.SelectMany(y => y.Value).ToArray(), SymbolEqualityComparer.Default);
    }
}