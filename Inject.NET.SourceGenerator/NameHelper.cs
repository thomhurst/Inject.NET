using System.Collections.Concurrent;
using System.Text;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Inject.NET.SourceGenerator;

public class NameHelper
{
    private static readonly ConcurrentDictionary<string, string> _propertyNameCache = new();
    private static readonly ConcurrentDictionary<string, string> _fieldNameCache = new();
    private static readonly Dictionary<char, string> CharReplacements = new()
    {
        ['<'] = "_",
        ['>'] = "_",
        [','] = "_",
        [' '] = "_",
        ['?'] = ""
    };

    private static readonly SymbolDisplayFormat ShortTypeNameFormat = new SymbolDisplayFormat(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    public static string AsProperty(ServiceModel serviceModel)
    {
        var cacheKey = $"{serviceModel.ServiceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}__{serviceModel.TenantName}__{serviceModel.Index}__{serviceModel.Key}";

        return _propertyNameCache.GetOrAdd(cacheKey, _ =>
        {
            var shortTypeName = serviceModel.ServiceType.ToDisplayString(ShortTypeNameFormat);
            return GeneratePropertyName(shortTypeName, cacheKey);
        });
    }

    private static string GeneratePropertyName(string shortTypeName, string cacheKey)
    {
        var parts = cacheKey.Split(new[] { "__" }, StringSplitOptions.None);
        var tenantName = parts[1];
        var index = parts[2];
        var serviceKey = parts.Length > 3 ? parts[3] : null;

        StringBuilder sb = new(shortTypeName.Length + 16);

        foreach (var c in shortTypeName)
        {
            if (CharReplacements.TryGetValue(c, out var replacement))
            {
                sb.Append(replacement);
            }
            else if (c == '.')
            {
                sb.Append("_");
            }
            else
            {
                sb.Append(c);
            }
        }

        var typeString = sb.ToString();

        string propertyName;
        if (!string.IsNullOrEmpty(tenantName))
        {
            propertyName = $"{typeString}_{tenantName}_{index}";
        }
        else
        {
            propertyName = $"{typeString}_{index}";
        }

        return !string.IsNullOrEmpty(serviceKey) ? $"Keyed_{propertyName}_{serviceKey}" : propertyName;
    }

    public static string AsField(ServiceModel serviceModel)
    {
        var propertyName = AsProperty(serviceModel);

        return _fieldNameCache.GetOrAdd(propertyName, static name =>
            $"_{name[..1].ToLower()}{name[1..]}");
    }
}