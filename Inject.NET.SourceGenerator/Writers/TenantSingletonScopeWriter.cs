using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class TenantSingletonScopeWriter
{
    public static void Write(SourceProductionContext sourceProductionContext, SourceCodeWriter sourceCodeWriter,
        Compilation compilation, TypedServiceProviderModel serviceProviderModel, ServiceModelCollection serviceModelCollection, Tenant tenant)
    {
        var className = $"SingletonScope_{tenant.Guid}";

        sourceCodeWriter.WriteLine($"public class {className} : global::Inject.NET.Services.SingletonScope<{className}, ServiceProvider_{tenant.Guid}, ServiceScope_{tenant.Guid}, SingletonScope_, ServiceScope_, ServiceProvider_>");
        sourceCodeWriter.WriteLine("{");

        var singletonModels = GetSingletonModels(tenant.TenantDependencies).ToArray();
        
        sourceCodeWriter.WriteLine(
            $"public {className}(ServiceProvider_{tenant.Guid} serviceProvider, ServiceFactories serviceFactories, SingletonScope_ parentScope) : base(serviceProvider, serviceFactories, parentScope)");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine("}");
        
        foreach (var serviceModel in singletonModels)
        {
            sourceCodeWriter.WriteLine();
            sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
            var propertyName = PropertyNameHelper.Format(serviceModel);
                    
            sourceCodeWriter.WriteLine(
                $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => field ??= Register({ObjectConstructionHelper.ConstructNewObject(serviceProviderModel.Type, serviceModelCollection.Services, serviceModel, Lifetime.Singleton)});");
        }

        foreach (var (_, serviceModels) in serviceModelCollection.Services)
        {
            var serviceModel = serviceModels.Last();

            if (serviceModel.Lifetime == Lifetime.Singleton || serviceModel.IsOpenGeneric)
            {
                continue;
            }

            var propertyName = PropertyNameHelper.Format(serviceModel);
            sourceCodeWriter.WriteLine(
                $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => ServiceProvider.Singletons.{propertyName};");
        }

        sourceCodeWriter.WriteLine("}");
    }
    
    private static IEnumerable<ServiceModel> GetSingletonModels(IDictionary<ISymbol?, List<ServiceModel>> dependencies)
    {
        foreach (var (_, serviceModels) in dependencies)
        {
            var serviceModel = serviceModels.Last();

            if (serviceModel.Lifetime != Lifetime.Singleton || serviceModel.IsOpenGeneric)
            {
                continue;
            }

            yield return serviceModel;
        }
    }
}