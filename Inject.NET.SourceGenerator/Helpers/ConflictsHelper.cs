using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Helpers;

public static class ConflictsHelper
{
    public static void CheckConflicts(this SourceProductionContext context, ServiceModel serviceModel, IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies)
    {
        if (serviceModel.GetAllNestedParameters(dependencies).Any(x =>
                SymbolEqualityComparer.Default.Equals(x.ServiceType, serviceModel.ServiceType)))
        {
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("1", "Conflict", "Conflict", "", DiagnosticSeverity.Error, true), null));
        }
    }
}