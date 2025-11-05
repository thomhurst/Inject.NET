using Inject.NET.SourceGenerator.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class ScopeWriter
{
    /// <summary>
    /// Writes the complete ServiceScope_ class implementation.
    /// </summary>
    /// <param name="sourceCodeWriter">The source code writer to write to.</param>
    /// <param name="serviceProviderModel">The service provider model containing type information.</param>
    /// <param name="rootServiceModelCollection">The collection of all root service models.</param>
    public static void Write(SourceCodeWriter sourceCodeWriter, TypedServiceProviderModel serviceProviderModel,
        RootServiceModelCollection rootServiceModelCollection, IDictionary<ServiceModelCollection.ServiceKey, List<DecoratorModel>> decorators = null)
    {
        sourceCodeWriter.WriteLine(
            $"public class ServiceScope_ : global::Inject.NET.Services.ServiceScope<{serviceProviderModel.Prefix}ServiceScope_, {serviceProviderModel.Prefix}ServiceProvider_, {serviceProviderModel.Prefix}SingletonScope_, {serviceProviderModel.Prefix}ServiceScope_, {serviceProviderModel.Prefix}SingletonScope_, {serviceProviderModel.Prefix}ServiceProvider_>");
        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine(
            $"public ServiceScope_({serviceProviderModel.Prefix}ServiceProvider_ serviceProvider, ServiceFactories serviceFactories) : base(serviceProvider, serviceFactories, null)");
        sourceCodeWriter.WriteLine("{");
        sourceCodeWriter.WriteLine("}");

        WriteProperties(sourceCodeWriter, rootServiceModelCollection, decorators);

        // REDESIGN: Removed GetService/GetServices overrides
        // Services are now resolved purely through dictionary lookup in base class
        // Properties above still provide direct access optimization
        // WriteGetServiceMethod(sourceCodeWriter, rootServiceModelCollection);
        // WriteGetServicesMethod(sourceCodeWriter, rootServiceModelCollection);

        sourceCodeWriter.WriteLine("}");
    }

    /// <summary>
    /// Writes all service properties for the scope, including individual service properties and enumerable collections.
    /// </summary>
    /// <param name="sourceCodeWriter">The source code writer to write to.</param>
    /// <param name="rootServiceModelCollection">The collection of all root service models.</param>
    private static void WriteProperties(SourceCodeWriter sourceCodeWriter, RootServiceModelCollection rootServiceModelCollection, IDictionary<ServiceModelCollection.ServiceKey, List<DecoratorModel>> decorators)
    {
        foreach (var (_, serviceModels) in rootServiceModelCollection.Services)
        {
            foreach (var serviceModel in serviceModels.Where(serviceModel => !serviceModel.IsOpenGeneric))
            {
                sourceCodeWriter.WriteLine();
                var propertyName = serviceModel.GetPropertyName();

                if (serviceModel.Lifetime != Lifetime.Scoped)
                {
                    sourceCodeWriter.WriteLine(
                        $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => {GetInvocation(rootServiceModelCollection, serviceModel, decorators)};");
                }
                else
                {
                    var fieldName = NameHelper.AsField(serviceModel);
                    sourceCodeWriter.WriteLine($"private {serviceModel.ServiceType.GloballyQualified()}? {fieldName};");

                    sourceCodeWriter.WriteLine(
                        $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => {fieldName} ??= {GetInvocation(rootServiceModelCollection, serviceModel, decorators)};");
                }
            }

            var model = serviceModels[^1];

            if (model.IsOpenGeneric)
            {
                continue;
            }
            
            var enumerablePropertyName = $"{model.GetPropertyName()}Enumerable";
            
            var arrayParts = serviceModels
                .Where(serviceModel => !serviceModel.IsOpenGeneric)
                .Select(serviceModel => serviceModel.GetPropertyName());
            
            sourceCodeWriter.WriteLine(
                $"public IReadOnlyList<{model.ServiceType.GloballyQualified()}> {enumerablePropertyName} => [{string.Join(", ", arrayParts)}];");
        }
    }

    /// <summary>
    /// Writes the GetService method override that resolves individual services by service key.
    /// </summary>
    /// <param name="sourceCodeWriter">The source code writer to write to.</param>
    /// <param name="rootServiceModelCollection">The collection of all root service models.</param>
    private static void WriteGetServiceMethod(SourceCodeWriter sourceCodeWriter, RootServiceModelCollection rootServiceModelCollection)
    {
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("public override object? GetService(global::Inject.NET.Models.ServiceKey serviceKey, Inject.NET.Interfaces.IServiceScope originatingScope)");
        sourceCodeWriter.WriteLine("{");
        
        foreach (var (serviceKey, serviceModels) in rootServiceModelCollection.Services)
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
            
            var key = serviceKey.Key is null ? "null" : $"\"{serviceKey.Key}\"";
            sourceCodeWriter.WriteLine($"if (serviceKey.Key == {key} && global::Inject.NET.Helpers.TypeHelper.IsEnumerable<{serviceModel.ServiceType.GloballyQualified()}>(serviceKey.Type))");
            sourceCodeWriter.WriteLine("{");
            sourceCodeWriter.WriteLine($"return {serviceModel.GetPropertyName()}Enumerable;");
            sourceCodeWriter.WriteLine("}");
        }
        
        sourceCodeWriter.WriteLine("return base.GetService(serviceKey, originatingScope);");
        sourceCodeWriter.WriteLine("}");
    }

    /// <summary>
    /// Writes the GetServices method override that resolves collections of services by service key.
    /// </summary>
    /// <param name="sourceCodeWriter">The source code writer to write to.</param>
    /// <param name="rootServiceModelCollection">The collection of all root service models.</param>
    private static void WriteGetServicesMethod(SourceCodeWriter sourceCodeWriter, RootServiceModelCollection rootServiceModelCollection)
    {
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("public override IReadOnlyList<object> GetServices(global::Inject.NET.Models.ServiceKey serviceKey, Inject.NET.Interfaces.IServiceScope originatingScope)");
        sourceCodeWriter.WriteLine("{");
        
        foreach (var (_, serviceModels) in rootServiceModelCollection.Services)
        {
            var first = serviceModels[0];
            
            sourceCodeWriter.WriteLine($"if (serviceKey == {first.GetNewServiceKeyInvocation()})");
            sourceCodeWriter.WriteLine("{");

            var arrayParts = serviceModels
                .Where(serviceModel => !serviceModel.IsOpenGeneric)
                .Select(serviceModel => serviceModel.GetPropertyName());

            sourceCodeWriter.WriteLine($"return [{string.Join(", ", arrayParts)}];");
            sourceCodeWriter.WriteLine("}");
        }

        sourceCodeWriter.WriteLine("return base.GetServices(serviceKey, originatingScope);");
        sourceCodeWriter.WriteLine("}");
    }

    /// <summary>
    /// Gets the appropriate invocation string for creating or accessing a service instance.
    /// </summary>
    /// <param name="rootServiceModelCollection">The collection of all root service models.</param>
    /// <param name="serviceModel">The service model to get invocation for.</param>
    /// <returns>The invocation string for the service.</returns>
    private static string GetInvocation(RootServiceModelCollection rootServiceModelCollection,
        ServiceModel serviceModel, IDictionary<ServiceModelCollection.ServiceKey, List<DecoratorModel>> decorators)
    {
        if (serviceModel.Lifetime == Lifetime.Singleton)
        {
            return $"Singletons.{serviceModel.GetPropertyName()}";
        }

        var constructNewObject = ObjectConstructionHelper.ConstructNewObject(rootServiceModelCollection.ServiceProviderType,
            rootServiceModelCollection.Services, serviceModel, serviceModel.Lifetime);
        
        // Check if there are decorators for this service
        if (decorators != null && decorators.TryGetValue(serviceModel.ServiceKey, out var decoratorList) && decoratorList.Count > 0)
        {
            // Wrap the base implementation with decorators
            var wrappedInvocation = constructNewObject;
            
            foreach (var decorator in decoratorList)
            {
                wrappedInvocation = WrapWithDecorator(rootServiceModelCollection, decorator, wrappedInvocation);
            }
            
            return $"Register<{serviceModel.ServiceType.GloballyQualified()}>({wrappedInvocation})";
        }
        
        return $"Register<{serviceModel.ServiceType.GloballyQualified()}>({constructNewObject})";
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
                    if (paramServiceModel.Lifetime == Lifetime.Singleton)
                    {
                        decoratorParams.Add($"Singletons.{paramServiceModel.GetPropertyName()}");
                    }
                    else
                    {
                        decoratorParams.Add(paramServiceModel.GetPropertyName());
                    }
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