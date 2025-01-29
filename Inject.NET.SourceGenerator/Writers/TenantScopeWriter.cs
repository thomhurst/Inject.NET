using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class TenantScopeWriter
{
    public static void Write(SourceProductionContext sourceProductionContext, SourceCodeWriter sourceCodeWriter,
        Compilation compilation, TypedServiceProviderModel serviceProviderModel, TenantedServiceModelCollection tenantedServiceModelCollection, Tenant tenant)
    {
        var className = $"ServiceScope_{tenant.TenantDefinition.Name}";
                
        sourceCodeWriter.WriteLine(
            $"public class {className} : global::Inject.NET.Services.ServiceScope<{className}, ServiceProvider_{tenant.TenantDefinition.Name}, SingletonScope_{tenant.TenantDefinition.Name}, ServiceScope_, SingletonScope_, ServiceProvider_>");
        sourceCodeWriter.WriteLine("{");

        var tenantServices = tenantedServiceModelCollection.Tenants[tenant.TenantDefinition.GloballyQualified()].Services;
        
        var models = GetModels(tenantServices).ToArray();

        sourceCodeWriter.WriteLine(
            $"public {className}(ServiceProvider_{tenant.TenantDefinition.Name} serviceProvider, ServiceFactories serviceFactories, ServiceScope_ parentScope) : base(serviceProvider, serviceFactories, parentScope)");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine("}");

        foreach (var serviceModel in models.Where(x => x.Key is null))
        {
            sourceCodeWriter.WriteLine();

            var propertyName = PropertyNameHelper.Format(serviceModel);

            if (serviceModel.Lifetime == Lifetime.Scoped)
            {
                sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");

                sourceCodeWriter.WriteLine(
                    serviceModel.ResolvedFromParent
                        ? $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => field ??= Register({serviceModel.GetNewServiceKeyInvocation()}, ParentScope.{propertyName});"
                        : $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => field ??= Register({serviceModel.GetNewServiceKeyInvocation()}, {ObjectConstructionHelper.ConstructNewObject(tenantedServiceModelCollection.ServiceProviderType, tenantServices, serviceModel, Lifetime.Scoped)});");
            }

            if (serviceModel.Lifetime == Lifetime.Singleton)
            {
                sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");

                sourceCodeWriter.WriteLine(
                    $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => field ??= Register({serviceModel.GetNewServiceKeyInvocation()}, Singletons.{propertyName});");
            }

            if (serviceModel.Lifetime == Lifetime.Transient)
            {
                sourceCodeWriter.WriteLine(
                    $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => {ObjectConstructionHelper.ConstructNewObject(tenantedServiceModelCollection.ServiceProviderType, tenantServices, serviceModel, Lifetime.Transient)};");
            }
        }
        
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("public override object? GetService(ServiceKey serviceKey, IServiceScope originatingScope)");
        sourceCodeWriter.WriteLine("{");
        foreach (var (_, serviceModels) in tenantServices)
        {
            var serviceModel = serviceModels[^1];
            
            if (serviceModel.IsOpenGeneric)
            {
                continue;
            }
            
            sourceCodeWriter.WriteLine($"if (serviceKey == {serviceModel.GetNewServiceKeyInvocation()})");
            sourceCodeWriter.WriteLine("{");
            sourceCodeWriter.WriteLine($"return {serviceModel.GetPropertyName()};");
            sourceCodeWriter.WriteLine("}");
        }
        sourceCodeWriter.WriteLine("return base.GetService(serviceKey, originatingScope);");
        sourceCodeWriter.WriteLine("}");


        sourceCodeWriter.WriteLine("}");
    }
    
    private static IEnumerable<ServiceModel> GetModels(IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies)
    {
        foreach (var (_, serviceModels) in dependencies)
        {
            var serviceModel = serviceModels.Last();

            if (serviceModel.IsOpenGeneric)
            {
                continue;
            }

            yield return serviceModel;
        }
    }
}