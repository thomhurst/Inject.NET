using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class ScopeWriter
{
    public static void Write(SourceProductionContext sourceProductionContext, SourceCodeWriter sourceCodeWriter,
        Compilation compilation, TypedServiceProviderModel serviceProviderModel, TenantedServiceModelCollection tenantedServiceModelCollection)
    {
        sourceCodeWriter.WriteLine(
            "public class ServiceScope_ : global::Inject.NET.Services.ServiceScope<ServiceScope_, ServiceProvider_, SingletonScope_, ServiceScope_, SingletonScope_, ServiceProvider_>");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine(
            "public ServiceScope_(ServiceProvider_ serviceProvider, ServiceFactories serviceFactories) : base(serviceProvider, serviceFactories, null)");
        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine("}");

        foreach (var (_, serviceModels) in tenantedServiceModelCollection.Services)
        {
            var serviceModel = serviceModels[^1];

            if (serviceModel.IsOpenGeneric)
            {
                continue;
            }
            
            sourceCodeWriter.WriteLine();
            sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
            var propertyName = PropertyNameHelper.Format(serviceModel);
            sourceCodeWriter.WriteLine($"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => field ??= {GetInvocation(tenantedServiceModelCollection, serviceModel)};");
        }
        
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("public override object? GetService(ServiceKey serviceKey, IServiceScope originatingScope)");
        sourceCodeWriter.WriteLine("{");
        foreach (var (_, serviceModels) in tenantedServiceModelCollection.Services)
        {
            var serviceModel = serviceModels[^1];
            
            if (serviceModel.IsOpenGeneric)
            {
                continue;
            }

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

    private static string GetInvocation(TenantedServiceModelCollection tenantedServiceModelCollection,
        ServiceModel serviceModel)
    {
        if (serviceModel.Lifetime == Lifetime.Scoped)
        {
            return
                $"Register({serviceModel.GetKey()}, {ObjectConstructionHelper.ConstructNewObject(tenantedServiceModelCollection.ServiceProviderType,
                    tenantedServiceModelCollection.Services, serviceModel, Lifetime.Scoped)})";
        }
        
        if (serviceModel.Lifetime == Lifetime.Singleton)
        {
            return $"Singletons.{PropertyNameHelper.Format(serviceModel)}";
        }
        
        return ObjectConstructionHelper.ConstructNewObject(tenantedServiceModelCollection.ServiceProviderType,
            tenantedServiceModelCollection.Services, serviceModel, Lifetime.Transient);
    }
}