using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class TenantSingletonScopeWriter
{
    public static void Write(SourceProductionContext sourceProductionContext,
        Compilation compilation, TypedServiceProviderModel serviceProviderModel, ServiceProviderInformation serviceProviderInformation, Tenant tenant)
    {
        NestedServiceWrapperWriter.Wrap(sourceProductionContext, serviceProviderModel,
            sourceCodeWriter =>
            {
                var className = $"SingletonScope{tenant.Guid}";

                sourceCodeWriter.WriteLine($"public class {className} : global::Inject.NET.Services.TenantedSingletonScope<ServiceProvider, SingletonScope>");
                sourceCodeWriter.WriteLine("{");

                sourceCodeWriter.WriteLine(
                    $"public {className}(ServiceProvider{tenant.Guid} tenantServiceProvider, ServiceProvider root, ServiceFactories serviceFactories) : base(tenantServiceProvider, root, serviceFactories)");
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
            });
    }
}