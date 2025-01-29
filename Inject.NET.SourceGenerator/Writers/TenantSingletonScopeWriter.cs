using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class TenantSingletonScopeWriter
{
    public static void Write(SourceProductionContext sourceProductionContext, SourceCodeWriter sourceCodeWriter,
        Compilation compilation, TypedServiceProviderModel serviceProviderModel, TenantedServiceModelCollection tenantedServiceModelCollection, Tenant tenant)
    {
        var className = $"SingletonScope_{tenant.TenantDefinition.Name}";

        var tenantedServices = tenantedServiceModelCollection.Tenants[tenant.TenantDefinition.GloballyQualified()];

        sourceCodeWriter.WriteLine($"public class {className} : global::Inject.NET.Services.SingletonScope<{className}, ServiceProvider_{tenant.TenantDefinition.Name}, ServiceScope_{tenant.TenantDefinition.Name}, SingletonScope_, ServiceScope_, ServiceProvider_>");
        sourceCodeWriter.WriteLine("{");

        var singletonModels = GetSingletonModels(tenantedServices.Services).ToArray();
        
        sourceCodeWriter.WriteLine(
            $"public {className}(ServiceProvider_{tenant.TenantDefinition.Name} serviceProvider, ServiceFactories serviceFactories, SingletonScope_ parentScope) : base(serviceProvider, serviceFactories, parentScope)");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine("}");
        
        foreach (var serviceModel in singletonModels)
        {
            sourceCodeWriter.WriteLine();
            sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
            
            var propertyName = PropertyNameHelper.Format(serviceModel);

            sourceCodeWriter.WriteLine(serviceModel.ResolvedFromParent
                ? $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => field ??= ParentScope.{propertyName};"
                : $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => field ??= Register({ObjectConstructionHelper.ConstructNewObject(serviceProviderModel.Type, tenantedServices.Services, serviceModel, Lifetime.Singleton)});");
        }
        
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("public override object? GetService(ServiceKey serviceKey, IServiceScope originatingScope)");
        sourceCodeWriter.WriteLine("{");
        foreach (var serviceModel in singletonModels)
        {
            var propertyName = PropertyNameHelper.Format(serviceModel);
            
            sourceCodeWriter.WriteLine($"if (serviceKey == {serviceModel.GetKey()})");
            sourceCodeWriter.WriteLine("{");
            sourceCodeWriter.WriteLine($"return {propertyName};");
            sourceCodeWriter.WriteLine("}");
        }
        sourceCodeWriter.WriteLine("return base.GetService(serviceKey, originatingScope);");
        sourceCodeWriter.WriteLine("}");


        sourceCodeWriter.WriteLine("}");
    }
    
    private static IEnumerable<ServiceModel> GetSingletonModels(IDictionary<ISymbol?, List<ServiceModel>> dependencies)
    {
        foreach (var (_, serviceModels) in dependencies)
        {
            var serviceModel = serviceModels[^1];

            if (serviceModel.Lifetime != Lifetime.Singleton || serviceModel.IsOpenGeneric)
            {
                continue;
            }

            yield return serviceModel;
        }
    }
}