using System.Globalization;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class TenantServiceProviderWriter
{
    public static void Write(SourceProductionContext sourceProductionContext, SourceCodeWriter sourceCodeWriter,
        TypedServiceProviderModel serviceProviderModel, TenantedServiceProviderInformation serviceProviderInformation,
        Tenant tenant)
    {
        var serviceProviderType = serviceProviderInformation.ServiceProviderType;

        var className = $"ServiceProvider{tenant.Guid}";
                
        sourceCodeWriter.WriteLine(
            $"{serviceProviderType.DeclaredAccessibility.ToString().ToLower(CultureInfo.InvariantCulture)} partial class {className}({serviceProviderModel.Type.Name + serviceProviderModel.Id}.ServiceProvider rootServiceProvider, ServiceFactories serviceFactories) : global::Inject.NET.Services.TenantServiceProvider<{serviceProviderModel.Type.Name + serviceProviderModel.Id}.ServiceProvider, {serviceProviderModel.Type.Name + serviceProviderModel.Id}.SingletonScope{tenant.Guid}>(rootServiceProvider, serviceFactories)");
        sourceCodeWriter.WriteLine("{");
                
        sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
        sourceCodeWriter.WriteLine(
            $"public override SingletonScope{tenant.Guid} SingletonScope => field ??= new(this, Root, serviceFactories);");

        sourceCodeWriter.WriteLine(
            $"public override IServiceScope CreateScope() => new {serviceProviderModel.Type.Name + serviceProviderModel.Id}.Scope{tenant.Guid}(Root, SingletonScope, ServiceFactories);");
                
        sourceCodeWriter.WriteLine(
            $"public static ValueTask<{className}> BuildAsync() =>");
        sourceCodeWriter.WriteLine($"\tnew {serviceProviderModel.Type.Name + serviceProviderModel.Id}.ServiceRegistrar{tenant.Guid}().BuildAsync();");

        sourceCodeWriter.WriteLine("}");
    }
}