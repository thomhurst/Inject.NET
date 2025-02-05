using Inject.NET.SourceGenerator.Models;

namespace Inject.NET.SourceGenerator.Writers;

internal static class TenantScopeWriter
{
    public static void Write(SourceCodeWriter sourceCodeWriter, TypedServiceProviderModel serviceProviderModel,
        TenantServiceModelCollection tenantServices)
    {
        var tenantName = tenantServices.TenantName;

        var className = $"ServiceScope_{tenantName}";

        sourceCodeWriter.WriteLine(
            $"public class {className} : global::Inject.NET.Services.ServiceScope<{className}, ServiceProvider_{tenantName}, SingletonScope_{tenantName}, {serviceProviderModel.Prefix}ServiceScope_, {serviceProviderModel.Prefix}SingletonScope_, {serviceProviderModel.Prefix}ServiceProvider_>");
        sourceCodeWriter.WriteLine("{");

        var models = GetModels(tenantServices.Services).ToArray();

        sourceCodeWriter.WriteLine(
            $"public {className}(ServiceProvider_{tenantName} serviceProvider, ServiceFactories serviceFactories, {serviceProviderModel.Prefix}ServiceScope_ parentScope) : base(serviceProvider, serviceFactories, parentScope)");
        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine("}");

        foreach (var serviceModel in models)
        {
            sourceCodeWriter.WriteLine();

            var propertyName = PropertyNameHelper.Format(serviceModel);

            if (serviceModel.Lifetime == Lifetime.Scoped)
            {
                sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");

                sourceCodeWriter.WriteLine(
                    serviceModel.ResolvedFromParent
                        ? $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => ParentScope.{propertyName};"
                        : $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => field ??= Register<{serviceModel.ServiceType.GloballyQualified()}>({ObjectConstructionHelper.ConstructNewObject(serviceProviderModel.Type, tenantServices.Services, serviceModel, Lifetime.Scoped)});");
            }

            if (serviceModel.Lifetime == Lifetime.Singleton)
            {
                sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");

                sourceCodeWriter.WriteLine(
                    $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => Singletons.{propertyName};");
            }

            if (serviceModel.Lifetime == Lifetime.Transient)
            {
                sourceCodeWriter.WriteLine(
                    $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => {ObjectConstructionHelper.ConstructNewObject(serviceProviderModel.Type, tenantServices.Services, serviceModel, Lifetime.Transient)};");
            }
        }

        // GetService
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine(
            "public override object GetService(global::Inject.NET.Models.ServiceKey serviceKey, Inject.NET.Interfaces.IServiceScope originatingScope)");
        sourceCodeWriter.WriteLine("{");
        foreach (var serviceModels in models.GroupBy(x => x.ServiceKey))
        {
            var serviceModel = serviceModels.Last();
            
            sourceCodeWriter.WriteLine($"if (serviceKey == {serviceModel.GetNewServiceKeyInvocation()})");
            sourceCodeWriter.WriteLine("{");
            sourceCodeWriter.WriteLine($"return {serviceModel.GetPropertyName()};");
            sourceCodeWriter.WriteLine("}");
        }

        sourceCodeWriter.WriteLine("return base.GetService(serviceKey, originatingScope);");
        sourceCodeWriter.WriteLine("}");


        // GetServices
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine(
            "public override IReadOnlyList<object> GetServices(global::Inject.NET.Models.ServiceKey serviceKey, Inject.NET.Interfaces.IServiceScope originatingScope)");
        sourceCodeWriter.WriteLine("{");
        foreach (var (_, serviceModels) in tenantServices.Services)
        {
            var first = serviceModels[0];
            {
                sourceCodeWriter.WriteLine($"if (serviceKey == {first.GetNewServiceKeyInvocation()})");
                sourceCodeWriter.WriteLine("{");
                
                var arrayParts = serviceModels
                    .OrderBy(x => x.ResolvedFromParent ? 0 : 1)
                    .Where(serviceModel => !serviceModel.IsOpenGeneric)
                    .Select(serviceModel => serviceModel.GetPropertyName());

                sourceCodeWriter.WriteLine($"return [{string.Join(", ", arrayParts)}];");
                sourceCodeWriter.WriteLine("}");
            }
        }

        sourceCodeWriter.WriteLine("return base.GetServices(serviceKey, originatingScope);");
        sourceCodeWriter.WriteLine("}");

        sourceCodeWriter.WriteLine("}");
    }

    private static IEnumerable<ServiceModel> GetModels(
        IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies)
    {
        foreach (var (_, serviceModels) in dependencies)
        {
            foreach (var serviceModel in serviceModels
                         .OrderBy(x => x.ResolvedFromParent ? 0 : 1)
                         .Where(serviceModel => !serviceModel.IsOpenGeneric))
            {
                yield return serviceModel;
            }
        }
    }
}