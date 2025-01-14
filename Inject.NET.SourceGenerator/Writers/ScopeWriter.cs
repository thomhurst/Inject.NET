using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class ScopeWriter
{
    public static void Write(SourceProductionContext sourceProductionContext,
        Compilation compilation, TypedServiceProviderModel serviceProviderModel, ServiceProviderInformation serviceProviderInformation)
    {
        NestedServiceWrapperWriter.Wrap(sourceProductionContext, serviceProviderModel,
            sourceCodeWriter =>
            {
                sourceCodeWriter.WriteLine(
                    "public class Scope : global::Inject.NET.Services.ServiceScope<SingletonScope>");
                sourceCodeWriter.WriteLine("{");

                sourceCodeWriter.WriteLine(
                    "public Scope(ServiceProviderRoot<SingletonScope> root, SingletonScope singletonScope, ServiceFactories serviceFactories) : base(root, singletonScope, serviceFactories)");
                sourceCodeWriter.WriteLine("{");
                sourceCodeWriter.WriteLine("}");

                foreach (var (_, serviceModels) in serviceProviderInformation.Dependencies)
                {
                    var serviceModel = serviceModels.Last();

                    if (serviceModel.Lifetime != Lifetime.Scoped || serviceModel.IsOpenGeneric)
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

                    if (serviceModel.Lifetime == Lifetime.Scoped || serviceModel.IsOpenGeneric)
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