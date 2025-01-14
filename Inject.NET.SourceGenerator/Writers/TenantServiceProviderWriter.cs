using System.Globalization;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class TenantServiceProviderWriter
{
    public static void Write(SourceProductionContext sourceProductionContext,
        TypedServiceProviderModel serviceProviderModel, TenantedServiceProviderInformation serviceProviderInformation,
        Tenant tenant)
    {
        NestedServiceWrapperWriter.Wrap(sourceProductionContext, serviceProviderInformation.ServiceProviderType,
            sourceCodeWriter =>
            {
                var serviceProviderType = serviceProviderInformation.ServiceProviderType;

                var className = $"ServiceProvider{tenant.Guid}";
                
                sourceCodeWriter.WriteLine(
                    $"{serviceProviderType.DeclaredAccessibility.ToString().ToLower(CultureInfo.InvariantCulture)} partial class {className}(ServiceProviderRoot<SingletonScope> rootServiceProvider, ServiceFactories serviceFactories) : TenantServiceProvider<SingletonScope>(rootServiceProvider, serviceFactories)");
                sourceCodeWriter.WriteLine("{");
                
                sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
                sourceCodeWriter.WriteLine(
                    $$"""public override SingletonScope SingletonScope => field ??= new(this, root, serviceFactories);""");

                sourceCodeWriter.WriteLine(
                    $"public override IServiceScope CreateScope() => new Scope{tenant.Guid}(this, SingletonScope, ServiceFactories);");
                
                sourceCodeWriter.WriteLine(
                    $"public static ValueTask<{serviceProviderModel.Type.GloballyQualified()}> BuildAsync() =>");
                sourceCodeWriter.WriteLine($"\tnew ServiceRegistrar{tenant.Guid}().BuildAsync();");

                sourceCodeWriter.WriteLine("}");
            });
    }
}