using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Models;

public class TypedServiceProviderModel
{
    public required INamedTypeSymbol Type { get; init; }

    [field: AllowNull, MaybeNull] public string ServiceRegistrarName => field ??= $"{Prefix}ServiceRegistrar";
    [field: AllowNull, MaybeNull] public string ServiceScopeName => field ??= $"{Prefix}ServiceScope_";
    [field: AllowNull, MaybeNull] public string SingletonScopeName => field ??= $"{Prefix}SingletonScope_";
    [field: AllowNull, MaybeNull] public string ServiceProviderName => field ??= $"{Prefix}ServiceProvider_";

    [field: AllowNull, MaybeNull]
    public string Prefix => field ??= GetPrefix();

    private string GetPrefix()
    {
        return $"{Type.GloballyQualified()}.";
        var parent = Type;

        List<string> parts = [];
        
        while (parent is not null)
        {
            parts.Add(parent.Name);
            parent = parent.ContainingType;
        }

        parts.Reverse();
        
        var prefix = string.Join(".", parts);
        
        if (Type.ContainingNamespace is { IsGlobalNamespace: true } or null)
        {
            return $"{prefix}.";
        }
        
        return $"global::{Type.ContainingNamespace.ToDisplayString()}.{prefix}.";
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TypedServiceProviderModel);
    }

    public override int GetHashCode()
    {
        return Type.GloballyQualified().GetHashCode();
    }

    private bool Equals(TypedServiceProviderModel? other)
    {
        return Type.GloballyQualified().Equals(other?.Type.GloballyQualified());
    }
}