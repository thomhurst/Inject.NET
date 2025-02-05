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
            sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
            
            var propertyName = PropertyNameHelper.Format(serviceModel);

            sourceCodeWriter.WriteLine(serviceModel.ResolvedFromParent
                ? $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => ParentScope.{propertyName};"
                : $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => field ??= Register<{serviceModel.ServiceType.GloballyQualified()}>({ObjectConstructionHelper.ConstructNewObject(serviceProviderModel.Type, tenantServices.Services, serviceModel, Lifetime.Singleton)});");
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
            foreach (var serviceModel in serviceModels)
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