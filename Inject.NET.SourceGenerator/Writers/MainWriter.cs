using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class MainWriter
{
    public static void GenerateServiceProviderCode(SourceProductionContext sourceProductionContext, TypedServiceProviderModel serviceProviderModel, Compilation compilation)
    {
        var dependencyInjectionAttributeType = compilation.GetTypeByMetadataName("Inject.NET.Attributes.IDependencyInjectionAttribute");

        var withTenantAttributeType = compilation.GetTypeByMetadataName("Inject.NET.Attributes.WithTenantAttribute`1");
        
        var attributes = serviceProviderModel.Type
            .GetAttributes();
        
        var dependencyAttributes = attributes
            .Where(x => x.AttributeClass?.AllInterfaces.Contains(dependencyInjectionAttributeType,
                SymbolEqualityComparer.Default) == true)
            .ToArray();
        
        var withTenantAttributes = attributes
            .Where(x => x.AttributeClass?.IsGenericType is true && SymbolEqualityComparer.Default.Equals(withTenantAttributeType, x.AttributeClass))
            .ToArray();

        var rootDependencies = DependencyDictionary.Create(compilation, dependencyAttributes);

        var tenants = TenantHelper.ConstructTenants(compilation, withTenantAttributes, rootDependencies);

        var serviceProviderInformation = TypeCollector.Collect(serviceProviderModel, compilation);
        
        ServiceRegistrarWriter.Write(sourceProductionContext, compilation, serviceProviderModel, rootDependencies);
        SingletonScopeWriter.Write(sourceProductionContext, compilation, serviceProviderInformation);
        ScopeWriter.Write(sourceProductionContext, compilation, serviceProviderInformation);
        ServiceProviderWriter.Write(sourceProductionContext, serviceProviderModel, serviceProviderInformation, []);
        
        foreach (var tenant in tenants)
        {
            TenantServiceRegistrarWriter.Write(sourceProductionContext, compilation, serviceProviderModel, tenant);
            TenantSingletonScopeWriter.Write(sourceProductionContext, compilation, serviceProviderInformation, tenant);
            TenantScopeWriter.Write(sourceProductionContext, compilation, serviceProviderInformation, tenant);
            TenantServiceProviderWriter.Write(sourceProductionContext, serviceProviderModel, serviceProviderInformation, tenant);
        }
    }
}