using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class TenantSingletonScopeWriter
{
    public static void Write(SourceProductionContext sourceProductionContext, SourceCodeWriter sourceCodeWriter,
        Compilation compilation, TypedServiceProviderModel serviceProviderModel, ServiceProviderInformation serviceProviderInformation, Tenant tenant)
    {
        var className = $"SingletonScope_{tenant.Guid}";

        sourceCodeWriter.WriteLine($"public class {className} : global::Inject.NET.Services.TenantedSingletonScope<{className}, ServiceProvider_, SingletonScope_, ServiceScope_>");
        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine(
            $"public {className}(ServiceProvider_{tenant.Guid} tenantServiceProvider, ServiceProvider_ root, ServiceFactories serviceFactories) : base(tenantServiceProvider, root, serviceFactories)");
        sourceCodeWriter.WriteLine("{");
        sourceCodeWriter.WriteLine("}");

        foreach (var (_, serviceModels) in tenant.TenantDependencies)
        {
            var serviceModel = serviceModels.Last();

            if (serviceModel.Lifetime != Lifetime.Singleton || serviceModel.IsOpenGeneric)
            {
                continue;
            }

            sourceCodeWriter.WriteLine();
            sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
            var propertyName = PropertyNameHelper.Format(serviceModel);
                    
            sourceCodeWriter.WriteLine(
                $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => field ??= Register({serviceModel.GetKey()}, {TypeHelper.GetOrConstructType(serviceProviderInformation.ServiceProviderType, tenant.TenantDependencies, tenant.RootDependencies, serviceModel, Lifetime.Singleton)});");
        }

        foreach (var (_, serviceModels) in serviceProviderInformation.ParentDependencies
                     .Where(x => !serviceProviderInformation.Dependencies.Keys.Contains(x.Key,
                         SymbolEqualityComparer.Default)))
        {
            var serviceModel = serviceModels.Last();

            if (serviceModel.Lifetime == Lifetime.Singleton || serviceModel.IsOpenGeneric)
            {
                continue;
            }

            var propertyName = PropertyNameHelper.Format(serviceModel);
            sourceCodeWriter.WriteLine(
                $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => Root.SingletonScope.{propertyName};");
        }

        sourceCodeWriter.WriteLine("}");
    }
}