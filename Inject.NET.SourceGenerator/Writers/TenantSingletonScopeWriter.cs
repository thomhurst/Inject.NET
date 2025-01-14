using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class TenantSingletonScopeWriter
{
    public static void Write(SourceProductionContext sourceProductionContext,
        Compilation compilation, ServiceProviderInformation serviceProviderInformation, Tenant tenant)
    {
        NestedServiceWrapperWriter.Wrap(sourceProductionContext, serviceProviderInformation.ServiceProviderType,
            sourceCodeWriter =>
            {
                var className = $"SingletonScope{tenant.Guid}";

                sourceCodeWriter.WriteLine($"public class {className} : TenantedSingletonScope<SingletonScope>");
                sourceCodeWriter.WriteLine("{");

                sourceCodeWriter.WriteLine(
                    $"public {className}(TenantServiceProvider<TSingletonScope> tenantServiceProvider, ServiceProviderRoot<SingletonScope> root, ServiceFactories serviceFactories) : base(tenantServiceProvider, root, serviceFactories)");
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