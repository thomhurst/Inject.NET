using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class TenantServiceProviderWriter
{
    public static void Write(SourceCodeWriter sourceCodeWriter, RootServiceModelCollection rootServiceModelCollection,
        Tenant tenant)
    {
        var serviceProviderType = rootServiceModelCollection.ServiceProviderType;

        var className = $"ServiceProvider_{tenant.TenantDefinition.Name}";
                
        sourceCodeWriter.WriteLine(
            $"{serviceProviderType.DeclaredAccessibility.ToString().ToLower(CultureInfo.InvariantCulture)} partial class {className}(ServiceFactories serviceFactories, ServiceProvider_ parent) : global::Inject.NET.Services.ServiceProvider<{className}, SingletonScope_{tenant.TenantDefinition.Name}, ServiceScope_{tenant.TenantDefinition.Name}, ServiceProvider_, SingletonScope_, ServiceScope_>(serviceFactories, parent)");
        sourceCodeWriter.WriteLine("{");
                
        sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
        sourceCodeWriter.WriteLine(
            $"public override SingletonScope_{tenant.TenantDefinition.Name} Singletons => field ??= new(this, serviceFactories, parent.Singletons);");

        sourceCodeWriter.WriteLine(
            $"public override ServiceScope_{tenant.TenantDefinition.Name} CreateTypedScope() => new ServiceScope_{tenant.TenantDefinition.Name}(this, serviceFactories, parent.CreateTypedScope());");
                
        sourceCodeWriter.WriteLine(
            $"public static ValueTask<{className}> BuildAsync(ServiceProvider_ serviceProvider) =>");
        sourceCodeWriter.WriteLine($"\tnew ServiceRegistrar{tenant.TenantDefinition.Name}().BuildAsync(serviceProvider);");

        WriteInitializeAsync(sourceCodeWriter, tenant);
        
        sourceCodeWriter.WriteLine("}");
    }
    
    private static void WriteInitializeAsync(SourceCodeWriter sourceCodeWriter, Tenant tenant)
    {
        sourceCodeWriter.WriteLine("public override async ValueTask InitializeAsync()");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine("await Singletons.InitializeAsync();");
        
        sourceCodeWriter.WriteLine("await using var scope = CreateTypedScope();");
        
        foreach (var serviceModel in tenant.TenantDependencies.Where(x => x.Key.Type is INamedTypeSymbol { IsUnboundGenericType: false }).Select(x => x.Value[^1]))
        {
            if(serviceModel.Lifetime == Lifetime.Singleton)
            {
                sourceCodeWriter.WriteLine($"_ = Singletons.{PropertyNameHelper.Format(serviceModel)};");
            }
            else if(serviceModel.Lifetime == Lifetime.Scoped)
            {
                sourceCodeWriter.WriteLine($"_ = scope.{PropertyNameHelper.Format(serviceModel)};");
            }
        }
        
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("await base.InitializeAsync();");

        sourceCodeWriter.WriteLine("}");
    }
}