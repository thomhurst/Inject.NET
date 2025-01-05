using Inject.NET.SourceGenerator.Models;
using Inject.NET.SourceGenerator.Writers;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

[Generator]
public class DependenciesSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName("Inject.NET.Attributes.ServiceProviderAttribute",
                static (_, _) => true,
                (ctx, _) => new TypedServiceProviderModel
                {
                    Type = (INamedTypeSymbol) ctx.TargetSymbol
                })
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(provider, ServiceProviderWriter.GenerateServiceProviderCode);
    }
}