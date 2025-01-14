using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class SingletonScopeWriter
{
    public static void Write(SourceProductionContext sourceProductionContext,
        Compilation compilation, ServiceProviderInformation serviceProviderInformation, Tenant? tenant = null)
    {
        NestedServiceWrapperWriter.Wrap(sourceProductionContext, serviceProviderInformation.ServiceProviderType,
            sourceCodeWriter =>
            {
                sourceCodeWriter.WriteLine($"public class SingletonScope : SingletonScope");
                sourceCodeWriter.WriteLine("{");

                sourceCodeWriter.WriteLine(
                    $"public SingletonScope(ServiceProviderRoot<SingletonScope> root, ServiceFactories serviceFactories) : base(root, serviceFactories)");
                sourceCodeWriter.WriteLine("{");
                sourceCodeWriter.WriteLine("}");

                foreach (var (_, serviceModels) in serviceProviderInformation.Dependencies)
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
                        $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => field ??= Register({serviceModel.GetKey()}, {ObjectConstructionHelper.ConstructNewObject(serviceProviderInformation.ServiceProviderType, serviceProviderInformation.Dependencies, serviceProviderInformation.ParentDependencies, serviceModel, Lifetime.Singleton)});");
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