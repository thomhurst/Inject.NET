using System.Text;
using Inject.NET.SourceGenerator.Models;

namespace Inject.NET.SourceGenerator.Writers;

internal static class ScopeWriter
{
    public static void Write(SourceCodeWriter sourceCodeWriter, TypedServiceProviderModel serviceProviderModel, RootServiceModelCollection rootServiceModelCollection)
    {
        sourceCodeWriter.WriteLine(
            $"public class ServiceScope_ : global::Inject.NET.Services.ServiceScope<{serviceProviderModel.Prefix}ServiceScope_, {serviceProviderModel.Prefix}ServiceProvider_, {serviceProviderModel.Prefix}SingletonScope_, {serviceProviderModel.Prefix}ServiceScope_, {serviceProviderModel.Prefix}SingletonScope_, {serviceProviderModel.Prefix}ServiceProvider_>");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine(
            $"public ServiceScope_({serviceProviderModel.Prefix}ServiceProvider_ serviceProvider, ServiceFactories serviceFactories) : base(serviceProvider, serviceFactories, null)");
        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine("}");

        foreach (var (_, serviceModels) in rootServiceModelCollection.Services)
        {
            foreach (var serviceModel in serviceModels)
            {
                if (serviceModel.IsOpenGeneric)
                {
                    continue;
                }

                sourceCodeWriter.WriteLine();
                var propertyName = NameHelper.AsProperty(serviceModel);

                if (serviceModel.Lifetime != Lifetime.Scoped)
                {
                    sourceCodeWriter.WriteLine(
                        $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => {GetInvocation(rootServiceModelCollection, serviceModel)};");
                }
                else
                {
                    var fieldName = NameHelper.AsField(serviceModel);
                    sourceCodeWriter.WriteLine($"private {serviceModel.ServiceType.GloballyQualified()}? {fieldName};");
                    
                    sourceCodeWriter.WriteLine(
                        $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => {fieldName} ??= {GetInvocation(rootServiceModelCollection, serviceModel)};");
                }
            }
        }
        
        // GetService
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("public override object GetService(global::Inject.NET.Models.ServiceKey serviceKey, Inject.NET.Interfaces.IServiceScope originatingScope)");
        sourceCodeWriter.WriteLine("{");
        foreach (var (_, serviceModels) in rootServiceModelCollection.Services)
        {
            var serviceModel = serviceModels[^1];
            
            if (serviceModel.IsOpenGeneric)
            {
                continue;
            }
            
            sourceCodeWriter.WriteLine($"if (serviceKey == {serviceModel.GetNewServiceKeyInvocation()})");
            sourceCodeWriter.WriteLine("{");
            
            sourceCodeWriter.WriteLine($"return {serviceModel.GetPropertyName()};");

            sourceCodeWriter.WriteLine("}");
        }
        sourceCodeWriter.WriteLine("return base.GetService(serviceKey, originatingScope);");
        sourceCodeWriter.WriteLine("}");

        // GetServices
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("public override IReadOnlyList<object> GetServices(global::Inject.NET.Models.ServiceKey serviceKey, Inject.NET.Interfaces.IServiceScope originatingScope)");
        sourceCodeWriter.WriteLine("{");
        
        foreach (var (_, serviceModels) in rootServiceModelCollection.Services)
        {
            var first = serviceModels[0];
            
            sourceCodeWriter.WriteLine($"if (serviceKey == {first.GetNewServiceKeyInvocation()})");
            sourceCodeWriter.WriteLine("{");

            var arrayParts = serviceModels
                .Where(serviceModel => !serviceModel.IsOpenGeneric)
                .Select(serviceModel => serviceModel.GetPropertyName());

            sourceCodeWriter.WriteLine($"return [{string.Join(", ", arrayParts)}];");
            
            sourceCodeWriter.WriteLine("}");
        }

        sourceCodeWriter.WriteLine("return base.GetServices(serviceKey, originatingScope);");
        sourceCodeWriter.WriteLine("}");
        
        sourceCodeWriter.WriteLine("}");
    }

    private static string GetInvocation(RootServiceModelCollection rootServiceModelCollection,
        ServiceModel serviceModel)
    {
        if (serviceModel.Lifetime == Lifetime.Scoped)
        {
            return
                $"Register<{serviceModel.ServiceType.GloballyQualified()}>({ObjectConstructionHelper.ConstructNewObject(rootServiceModelCollection.ServiceProviderType,
                    rootServiceModelCollection.Services, serviceModel, Lifetime.Scoped)})";
        }
        
        if (serviceModel.Lifetime == Lifetime.Singleton)
        {
            return $"Singletons.{NameHelper.AsProperty(serviceModel)}";
        }
        
        return $"Register<{serviceModel.ServiceType.GloballyQualified()}>({ObjectConstructionHelper.ConstructNewObject(rootServiceModelCollection.ServiceProviderType,
            rootServiceModelCollection.Services, serviceModel, Lifetime.Transient)})";
    }
}