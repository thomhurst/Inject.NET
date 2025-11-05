using Inject.NET.SourceGenerator.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class SingletonScopeWriter
{
    public static void Write(SourceCodeWriter sourceCodeWriter, TypedServiceProviderModel serviceProviderModel, RootServiceModelCollection rootServiceModelCollection, IDictionary<ServiceModelCollection.ServiceKey, List<DecoratorModel>> decorators = null)
    {
        sourceCodeWriter.WriteLine($"public class SingletonScope_ : global::Inject.NET.Services.SingletonScope<{serviceProviderModel.Prefix}SingletonScope_, {serviceProviderModel.Prefix}ServiceProvider_, {serviceProviderModel.Prefix}ServiceScope_, {serviceProviderModel.Prefix}SingletonScope_, {serviceProviderModel.Prefix}ServiceScope_, {serviceProviderModel.Prefix}ServiceProvider_>");
        sourceCodeWriter.WriteLine("{");
        
        var singletonModels = GetSingletonModels(rootServiceModelCollection.Services).ToArray();

        sourceCodeWriter.WriteLine(
            $"public SingletonScope_({serviceProviderModel.Prefix}ServiceProvider_ serviceProvider, ServiceFactories serviceFactories) : base(serviceProvider, serviceFactories, null)");
        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine("}");

        // REDESIGN: Removed GetService override
        // Services now resolved purely through dictionary lookup in base class
        // Properties below still provide direct access optimization
        // sourceCodeWriter.WriteLine("public override object GetService(global::Inject.NET.Models.ServiceKey serviceKey, Inject.NET.Interfaces.IServiceScope originatingScope)");
        // sourceCodeWriter.WriteLine("{");
        // foreach (var serviceModel in singletonModels)
        // {
        //     sourceCodeWriter.WriteLine($"if (serviceKey == {serviceModel.GetNewServiceKeyInvocation()})");
        //     sourceCodeWriter.WriteLine("{");
        //     sourceCodeWriter.WriteLine($"return {serviceModel.GetPropertyName()};");
        //     sourceCodeWriter.WriteLine("}");
        // }
        // sourceCodeWriter.WriteLine();
        // sourceCodeWriter.WriteLine("return base.GetService(serviceKey, originatingScope);");
        // sourceCodeWriter.WriteLine("}");

        foreach (var (_, serviceModels) in rootServiceModelCollection.Services)
        {
            foreach (var serviceModel in serviceModels.Where(serviceModel => serviceModel.Lifetime == Lifetime.Singleton && !serviceModel.IsOpenGeneric))
            {
                sourceCodeWriter.WriteLine();
                var propertyName = serviceModel.GetPropertyName();

                // Use GetServices to ensure single code path for singleton creation
                // This delegates to the dictionary-based resolution which handles caching, parent delegation, etc.
                sourceCodeWriter.WriteLine(
                    $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => ({serviceModel.ServiceType.GloballyQualified()})GetServices({serviceModel.GetNewServiceKeyInvocation()})[^1];");
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
    
    private static string WrapWithDecorator(RootServiceModelCollection rootServiceModelCollection, DecoratorModel decorator, string innerInvocation)
    {
        // Build decorator constructor invocation
        var decoratorParams = new List<string>();
        
        foreach (var param in decorator.Parameters)
        {
            // Check if this parameter is the decorated service
            if (SymbolEqualityComparer.Default.Equals(param.Type, decorator.ServiceType))
            {
                // This is the inner service parameter
                decoratorParams.Add(innerInvocation);
            }
            else
            {
                // This is another dependency - resolve it normally
                var paramServiceKey = new ServiceModelCollection.ServiceKey(param.Type, param.Key);
                if (rootServiceModelCollection.Services.TryGetValue(paramServiceKey, out var paramServiceModels))
                {
                    var paramServiceModel = paramServiceModels[^1];
                    decoratorParams.Add(paramServiceModel.GetPropertyName());
                }
                else
                {
                    // Try to resolve from service provider
                    decoratorParams.Add($"GetRequiredService<{param.Type.GloballyQualified()}>()");
                }
            }
        }
        
        return $"new {decorator.DecoratorType.GloballyQualified()}({string.Join(", ", decoratorParams)})";
    }
}