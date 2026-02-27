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

            var propertyName = serviceModel.GetPropertyName();

            if (serviceModel.Lifetime == Lifetime.Scoped)
            {
                if (serviceModel.ResolvedFromParent)
                {
                    sourceCodeWriter.WriteLine(
                        $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => ParentScope.{propertyName};");
                }
                else
                {
                    var fieldName = NameHelper.AsField(serviceModel);
                    sourceCodeWriter.WriteLine($"private {serviceModel.ServiceType.GloballyQualified()}? {fieldName};");

                    var constructNewObject = ObjectConstructionHelper.ConstructNewObject(serviceProviderModel.Type, tenantServices.Services, serviceModel, Lifetime.Scoped);
                    var invocation = serviceModel.ExternallyOwned
                        ? constructNewObject
                        : $"Register<{serviceModel.ServiceType.GloballyQualified()}>({constructNewObject})";

                    sourceCodeWriter.WriteLine(
                        $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => {fieldName} ??= {invocation};");
                }
            }

            if (serviceModel.Lifetime == Lifetime.Singleton)
            {
                sourceCodeWriter.WriteLine(
                    $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => Singletons.{propertyName};");
            }

            if (serviceModel.Lifetime == Lifetime.Transient)
            {
                sourceCodeWriter.WriteLine(
                    $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => {ObjectConstructionHelper.ConstructNewObject(serviceProviderModel.Type, tenantServices.Services, serviceModel, Lifetime.Transient)};");
            }
        }

        // GetServices override is necessary to combine parent and local services for multi-tenant scenarios
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