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
            var serviceModel = serviceModels[^1];

            if (serviceModel.IsOpenGeneric)
            {
                continue;
            }
            
            sourceCodeWriter.WriteLine();
            var propertyName = PropertyNameHelper.Format(serviceModel);

            if (serviceModel.Lifetime == Lifetime.Transient)
            {
                sourceCodeWriter.WriteLine(
                    $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => {GetInvocation(rootServiceModelCollection, serviceModel)};");
            }
            else
            {
                sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
                sourceCodeWriter.WriteLine(
                    $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => field ??= {GetInvocation(rootServiceModelCollection, serviceModel)};");
            }
        }
        
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

        sourceCodeWriter.WriteLine("}");
    }

    private static string GetInvocation(RootServiceModelCollection rootServiceModelCollection,
        ServiceModel serviceModel)
    {
        if (serviceModel.Lifetime == Lifetime.Scoped)
        {
            return
                $"Register<{serviceModel.ServiceType.GloballyQualified()}>({serviceModel.GetNewServiceKeyInvocation()}, {ObjectConstructionHelper.ConstructNewObject(rootServiceModelCollection.ServiceProviderType,
                    rootServiceModelCollection.Services, serviceModel, Lifetime.Scoped)})";
        }
        
        if (serviceModel.Lifetime == Lifetime.Singleton)
        {
            return $"Singletons.{PropertyNameHelper.Format(serviceModel)}";
        }
        
        return ObjectConstructionHelper.ConstructNewObject(rootServiceModelCollection.ServiceProviderType,
            rootServiceModelCollection.Services, serviceModel, Lifetime.Transient);
    }
}