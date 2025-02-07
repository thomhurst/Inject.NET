using Inject.NET.SourceGenerator.Models;

namespace Inject.NET.SourceGenerator.Writers;

internal static class SingletonScopeWriter
{
    public static void Write(SourceCodeWriter sourceCodeWriter, TypedServiceProviderModel serviceProviderModel, RootServiceModelCollection rootServiceModelCollection)
    {
        sourceCodeWriter.WriteLine($"public class SingletonScope_ : global::Inject.NET.Services.SingletonScope<{serviceProviderModel.Prefix}SingletonScope_, {serviceProviderModel.Prefix}ServiceProvider_, {serviceProviderModel.Prefix}ServiceScope_, {serviceProviderModel.Prefix}SingletonScope_, {serviceProviderModel.Prefix}ServiceScope_, {serviceProviderModel.Prefix}ServiceProvider_>");
        sourceCodeWriter.WriteLine("{");
        
        var singletonModels = GetSingletonModels(rootServiceModelCollection.Services).ToArray();

        sourceCodeWriter.WriteLine(
            $"public SingletonScope_({serviceProviderModel.Prefix}ServiceProvider_ serviceProvider, ServiceFactories serviceFactories) : base(serviceProvider, serviceFactories, null)");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine("}");
        
        sourceCodeWriter.WriteLine("public override object GetService(global::Inject.NET.Models.ServiceKey serviceKey, Inject.NET.Interfaces.IServiceScope originatingScope)");
        sourceCodeWriter.WriteLine("{");
        foreach (var serviceModel in singletonModels)
        {
            sourceCodeWriter.WriteLine($"if (serviceKey == {serviceModel.GetNewServiceKeyInvocation()})");
            sourceCodeWriter.WriteLine("{");
            sourceCodeWriter.WriteLine($"return {serviceModel.GetPropertyName()};");
            sourceCodeWriter.WriteLine("}");
        }
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("return base.GetService(serviceKey, originatingScope);");
        sourceCodeWriter.WriteLine("}");


        foreach (var (_, serviceModels) in rootServiceModelCollection.Services)
        {
            foreach (var serviceModel in serviceModels.Where(serviceModel => serviceModel.Lifetime == Lifetime.Singleton && !serviceModel.IsOpenGeneric))
            {
                sourceCodeWriter.WriteLine();
                var fieldName = NameHelper.AsField(serviceModel);
                sourceCodeWriter.WriteLine($"private {serviceModel.ServiceType.GloballyQualified()}? {fieldName};");
                var propertyName = serviceModel.GetPropertyName();

                sourceCodeWriter.WriteLine(
                    $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => {fieldName} ??= Register<{serviceModel.ServiceType.GloballyQualified()}>({ObjectConstructionHelper.ConstructNewObject(rootServiceModelCollection.ServiceProviderType, rootServiceModelCollection.Services, serviceModel, Lifetime.Singleton)});");
            }
        }

        sourceCodeWriter.WriteLine("}");
    }
    
    private static IEnumerable<ServiceModel> GetSingletonModels(IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies)
    {
        foreach (var (_, serviceModels) in dependencies)
        {
            var serviceModel = serviceModels[^1];

            if (serviceModel.Lifetime != Lifetime.Singleton || serviceModel.IsOpenGeneric)
            {
                continue;
            }

            yield return serviceModel;
        }
    }
}