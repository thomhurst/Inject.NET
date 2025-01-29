using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class SingletonScopeWriter
{
    public static void Write(SourceProductionContext sourceProductionContext, SourceCodeWriter sourceCodeWriter,
        Compilation compilation, TypedServiceProviderModel serviceProviderModel, TenantedServiceModelCollection tenantedServiceModelCollection)
    {
        sourceCodeWriter.WriteLine("public class SingletonScope_ : global::Inject.NET.Services.SingletonScope<SingletonScope_, ServiceProvider_, ServiceScope_, SingletonScope_, ServiceScope_, ServiceProvider_>");
        sourceCodeWriter.WriteLine("{");
        
        var singletonModels = GetSingletonModels(tenantedServiceModelCollection.Services).ToArray();

        sourceCodeWriter.WriteLine(
            "public SingletonScope_(ServiceProvider_ serviceProvider, ServiceFactories serviceFactories) : base(serviceProvider, serviceFactories, null)");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine("}");
        
        sourceCodeWriter.WriteLine("public override object? GetService(ServiceKey serviceKey, IServiceScope originatingScope)");
        sourceCodeWriter.WriteLine("{");
        foreach (var serviceModel in singletonModels)
        {
            sourceCodeWriter.WriteLine($"if (serviceKey == {serviceModel.GetNewServiceKeyInvocation()})");
            sourceCodeWriter.WriteLine("{");
            sourceCodeWriter.WriteLine($"return {serviceModel.GetPropertyName()};");
            sourceCodeWriter.WriteLine("}");
        }
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("return base.GetService(serviceKey, originatingScope);");
        sourceCodeWriter.WriteLine("}");


        foreach (var serviceModel in singletonModels)
        {
            sourceCodeWriter.WriteLine();
            sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
            var propertyName = PropertyNameHelper.Format(serviceModel);
                    
            sourceCodeWriter.WriteLine(
                $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => field ??= Register({ObjectConstructionHelper.ConstructNewObject(tenantedServiceModelCollection.ServiceProviderType, tenantedServiceModelCollection.Services, serviceModel, Lifetime.Singleton)});");
        }

        sourceCodeWriter.WriteLine("}");
    }
    
    private static IEnumerable<ServiceModel> GetSingletonModels(IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies)
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