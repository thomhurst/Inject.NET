﻿using Inject.NET.SourceGenerator.Constants;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Helpers;

public static class ConflictsHelper
{
    public static bool HasConflicts(this SourceProductionContext context, ServiceModel serviceModel, IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies)
    {
        return HasConflicts(context, serviceModel, serviceModel, dependencies);
    }
    
    private static bool HasConflicts(this SourceProductionContext context, ServiceModel originatingServiceModel, ServiceModel serviceModel, IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies)
    {
        return serviceModel.Parameters.Any(parameter => IsConflict(context, dependencies, originatingServiceModel, serviceModel, parameter));
    }

    private static bool IsConflict(SourceProductionContext context,
        IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies,
        ServiceModel originatingServiceModel,
        ServiceModel serviceModel,
        Parameter parameter)
    {
        if (!dependencies.TryGetValue(parameter.ServiceKey, out var models))
        {
            return false;
        }
        
        return models.Any(model =>
        {
            if (SymbolEqualityComparer.Default.Equals(originatingServiceModel.ServiceType, model.ServiceType)
                || SymbolEqualityComparer.Default.Equals(originatingServiceModel.ImplementationType, model.ImplementationType))
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(DiagnosticCodes.CircularDependency, "Conflict", "Conflict: {0} depends on {1} which depends on {0}", "", DiagnosticSeverity.Error, true), null, originatingServiceModel.ImplementationType, serviceModel.ImplementationType));
                return true;
            }
            
            return HasConflicts(context, originatingServiceModel, model, dependencies);
        });
    }
}