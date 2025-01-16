using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Models;

public class TypedServiceProviderModel
{
    public required INamedTypeSymbol Type { get; init; }
    
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