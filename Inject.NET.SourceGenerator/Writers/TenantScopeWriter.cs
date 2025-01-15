using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class TenantScopeWriter
{
    public static void Write(SourceProductionContext sourceProductionContext, SourceCodeWriter sourceCodeWriter,
        Compilation compilation, TypedServiceProviderModel serviceProviderModel, ServiceProviderInformation serviceProviderInformation, Tenant tenant)
    {
        var className = $"ServiceScope_{tenant.Guid}";
                
        sourceCodeWriter.WriteLine(
            $"public class {className} : global::Inject.NET.Services.ServiceScope<{className}, ServiceProvider_{tenant.Guid}, SingletonScope_{tenant.Guid}, ServiceScope_, SingletonScope_, ServiceProvider_>");
        sourceCodeWriter.WriteLine("{");
        
        var scopedModels = GetScopedModels(tenant.TenantDependencies).ToArray();

        sourceCodeWriter.WriteLine(
            $"public {className}(ServiceProvider_{tenant.Guid} serviceProvider, ServiceFactories serviceFactories, ServiceScope_ parentScope) : base(serviceProvider, serviceFactories, parentScope)");
        sourceCodeWriter.WriteLine("{");
        
        foreach (var serviceModel in scopedModels)
        {
            var propertyName = PropertyNameHelper.Format(serviceModel);
            
            sourceCodeWriter.WriteLine($"Register({serviceModel.GetKey()}, () => {propertyName});");
        }
        
        sourceCodeWriter.WriteLine("}");

        foreach (var serviceModel in scopedModels)
        {
            sourceCodeWriter.WriteLine();
            sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
            var propertyName = PropertyNameHelper.Format(serviceModel);
            sourceCodeWriter.WriteLine(
                $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => field ??= Register({serviceModel.GetKey()}, {TypeHelper.GetOrConstructType(serviceProviderInformation.ServiceProviderType, serviceProviderInformation.Dependencies, serviceProviderInformation.ParentDependencies, serviceModel, Lifetime.Scoped)});");
        }

        foreach (var (_, serviceModels) in serviceProviderInformation.ParentDependencies
                     .Where(x => !serviceProviderInformation.Dependencies.Keys.Contains(x.Key,
                         SymbolEqualityComparer.Default)))
        {
            var serviceModel = serviceModels.Last();

            if (serviceModel.Lifetime == Lifetime.Scoped || serviceModel.IsOpenGeneric)
            {
                continue;
            }

            var propertyName = PropertyNameHelper.Format(serviceModel);
            sourceCodeWriter.WriteLine(
                $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => ServiceProvider.Singletons.{propertyName};");
        }

        sourceCodeWriter.WriteLine("}");
    }
    
    private static IEnumerable<ServiceModel> GetScopedModels(Dictionary<ISymbol?, ServiceModel[]> dependencies)
    {
        foreach (var (_, serviceModels) in dependencies)
        {
            var serviceModel = serviceModels.Last();

            if (serviceModel.Lifetime != Lifetime.Scoped || serviceModel.IsOpenGeneric)
            {
                continue;
            }

            yield return serviceModel;
        }
    }
}