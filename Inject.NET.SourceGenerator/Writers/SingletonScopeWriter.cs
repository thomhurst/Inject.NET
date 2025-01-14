using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class SingletonScopeWriter
{
    public static void Write(SourceProductionContext sourceProductionContext, SourceCodeWriter sourceCodeWriter,
        Compilation compilation, TypedServiceProviderModel serviceProviderModel, ServiceProviderInformation serviceProviderInformation)
    {
            {
                sourceCodeWriter.WriteLine("public class SingletonScope : global::Inject.NET.Services.SingletonScope");
                sourceCodeWriter.WriteLine("{");

                sourceCodeWriter.WriteLine(
                    "public SingletonScope(ServiceProvider root, ServiceFactories serviceFactories) : base(root, serviceFactories)");
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
            }
    }
}