﻿using System.Collections.Generic;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class TypeHelper
{
    public static string WriteType(
        INamedTypeSymbol serviceProviderType,
        Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary,
        ServiceModel serviceModel)
    {
        if (serviceModel.Lifetime == Lifetime.Transient)
        {
            return ObjectConstructionHelper.ConstructNewObject(serviceProviderType, dependencyDictionary, serviceModel);
        }

        if (serviceModel.Lifetime == Lifetime.Singleton)
        {
            return $"(({serviceProviderType.GloballyQualified()}SingletonScope)scope.SingletonScope).{PropertyNameHelper.Format(serviceModel)}.Value";
        }
        
        return $"(({serviceProviderType.GloballyQualified()}Scope)scope).{PropertyNameHelper.Format(serviceModel)}.Value";
    }
}