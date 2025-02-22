using Inject.NET.SourceGenerator.Models;

namespace Inject.NET.SourceGenerator.Writers;

internal static class TenantSingletonScopeWriter
{
    public static void Write(SourceCodeWriter sourceCodeWriter, TypedServiceProviderModel serviceProviderModel, TenantServiceModelCollection tenantServices)
    {
        var tenantName = tenantServices.TenantName;
        
        var className = $"SingletonScope_{tenantName}";
        
        sourceCodeWriter.WriteLine($"public class {className} : global::Inject.NET.Services.SingletonScope<{className}, ServiceProvider_{tenantName}, ServiceScope_{tenantName}, {serviceProviderModel.Prefix}SingletonScope_, {serviceProviderModel.Prefix}ServiceScope_, {serviceProviderModel.Prefix}ServiceProvider_>");
        sourceCodeWriter.WriteLine("{");

        var singletonModels = GetSingletonModels(tenantServices.Services).ToArray();
        
        sourceCodeWriter.WriteLine(
            $"public {className}(ServiceProvider_{tenantName} serviceProvider, ServiceFactories serviceFactories, {serviceProviderModel.Prefix}SingletonScope_ parentScope) : base(serviceProvider, serviceFactories, parentScope)");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine("}");
        
        foreach (var serviceModel in singletonModels)
        {
            sourceCodeWriter.WriteLine();
            
            var propertyName = serviceModel.GetPropertyName();

            if (serviceModel.ResolvedFromParent)
            {
                sourceCodeWriter.WriteLine(
                    $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => ParentScope.{propertyName};");
            }
            else
            {
                var fieldName = NameHelper.AsField(serviceModel);
                sourceCodeWriter.WriteLine($"private {serviceModel.ServiceType.GloballyQualified()}? {fieldName};");

                sourceCodeWriter.WriteLine(
                    $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => {fieldName} ??= Register<{serviceModel.ServiceType.GloballyQualified()}>({ObjectConstructionHelper.ConstructNewObject(serviceProviderModel.Type, tenantServices.Services, serviceModel, Lifetime.Singleton)});");
            }
        }
        
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("public override object GetService(global::Inject.NET.Models.ServiceKey serviceKey, Inject.NET.Interfaces.IServiceScope originatingScope)");
        sourceCodeWriter.WriteLine("{");
        
        foreach (var serviceModel in singletonModels)
        {
            sourceCodeWriter.WriteLine($"if (serviceKey == {serviceModel.GetNewServiceKeyInvocation()})");
            sourceCodeWriter.WriteLine("{");
            sourceCodeWriter.WriteLine($"return {serviceModel.GetPropertyName()};");
            sourceCodeWriter.WriteLine("}");
        }

        sourceCodeWriter.WriteLine("return base.GetService(serviceKey, originatingScope);");
        sourceCodeWriter.WriteLine("}");


        sourceCodeWriter.WriteLine("}");
    }
    
    private static IEnumerable<ServiceModel> GetSingletonModels(IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies)
    {
        foreach (var (_, serviceModels) in dependencies)
        {
            foreach (var serviceModel in serviceModels.OrderBy(x => x.ResolvedFromParent ? 0 : 1))
            {
                if (serviceModel.Lifetime != Lifetime.Singleton || serviceModel.IsOpenGeneric)
                {
                    continue;
                }

                yield return serviceModel;
            }
        }
    }
}