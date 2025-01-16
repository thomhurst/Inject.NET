using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class SingletonScopeWriter
{
    public static void Write(SourceProductionContext sourceProductionContext, SourceCodeWriter sourceCodeWriter,
        Compilation compilation, TypedServiceProviderModel serviceProviderModel, ServiceProviderInformation serviceProviderInformation)
    {
        sourceCodeWriter.WriteLine("public class SingletonScope_ : global::Inject.NET.Services.SingletonScope<SingletonScope_, ServiceProvider_, ServiceScope_, SingletonScope_, ServiceScope_, ServiceProvider_>");
        sourceCodeWriter.WriteLine("{");
        
        var singletonModels = GetSingletonModels(serviceProviderInformation.Dependencies).ToArray();

        sourceCodeWriter.WriteLine(
            "public SingletonScope_(ServiceProvider_ serviceProvider, ServiceFactories serviceFactories) : base(serviceProvider, serviceFactories, null)");
        sourceCodeWriter.WriteLine("{");
        
        foreach (var serviceModel in singletonModels)
        {
            var propertyName = PropertyNameHelper.Format(serviceModel);
            
            sourceCodeWriter.WriteLine($"Register({serviceModel.GetKey()}, () => {propertyName});");
        }
        
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
                $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => ServiceProvider.Singletons.{propertyName};");
        }

        sourceCodeWriter.WriteLine("}");
    }
    
    private static IEnumerable<ServiceModel> GetSingletonModels(Dictionary<ISymbol?, ServiceModel[]> dependencies)
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