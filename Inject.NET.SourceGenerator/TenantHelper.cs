﻿using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class TenantHelper
{
    public static Tenant[] ConstructTenants(Compilation compilation,
        AttributeData[] withTenantAttributes, IDictionary<ISymbol?, List<ServiceModel>> rootDependencies)
    {
        return ConstructTenantsEnumerable(compilation, withTenantAttributes, rootDependencies).ToArray();
    }

    private static IEnumerable<Tenant> ConstructTenantsEnumerable(Compilation compilation, AttributeData[] withTenantAttributes,
        IDictionary<ISymbol?, List<ServiceModel>> rootDependencies)
    {
        var dependencyInjectionAttributeType = compilation.GetTypeByMetadataName("Inject.NET.Attributes.IDependencyInjectionAttribute");

        foreach (var withTenantAttribute in withTenantAttributes)
        {
            var tenantId = (string)withTenantAttribute.ConstructorArguments[0].Value!;
            var definitionType = withTenantAttribute.AttributeClass!.TypeArguments[0];
            
            var attributes = definitionType.GetAttributes();
            
            var dependencyAttributes = attributes
                .Where(x => x.AttributeClass?.AllInterfaces.Contains(dependencyInjectionAttributeType,
                    SymbolEqualityComparer.Default) == true)
                .ToArray();

            var dependencies = DependencyDictionary.Create(compilation, dependencyAttributes);

            yield return new Tenant
            {
                TenantId = tenantId,
                TenantDefinition = (INamedTypeSymbol) definitionType,
                RootDependencies = rootDependencies,
                TenantDependencies = dependencies
            };
        }
    }
}