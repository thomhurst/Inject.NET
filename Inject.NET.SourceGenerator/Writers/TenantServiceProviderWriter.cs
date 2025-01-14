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

        var className = $"ServiceProvider_{tenant.Guid}";
                
        sourceCodeWriter.WriteLine(
            $"{serviceProviderType.DeclaredAccessibility.ToString().ToLower(CultureInfo.InvariantCulture)} partial class {className}(ServiceProvider_ rootServiceProvider, ServiceFactories serviceFactories) : global::Inject.NET.Services.TenantServiceProvider<SingletonScope_{tenant.Guid}, ServiceProvider_, SingletonScope_, ServiceScope_>(rootServiceProvider, serviceFactories)");
        sourceCodeWriter.WriteLine("{");
                
        sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
        sourceCodeWriter.WriteLine(
            $"public override SingletonScope_{tenant.Guid} SingletonScope => field ??= new(this, Root, serviceFactories);");

        sourceCodeWriter.WriteLine(
            $"public override IServiceScope CreateScope() => new ServiceScope{tenant.Guid}(Root, serviceFactories);");
                
        sourceCodeWriter.WriteLine(
            $"public static ValueTask<{className}> BuildAsync(ServiceProvider_ root) =>");
        sourceCodeWriter.WriteLine($"\tnew ServiceRegistrar{tenant.Guid}().BuildAsync(root);");

        sourceCodeWriter.WriteLine("}");
    }
}