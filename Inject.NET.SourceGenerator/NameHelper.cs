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

    public static string AsProperty(ServiceModel serviceModel)
    {
        var cacheKey = $"{serviceModel.ServiceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}__{serviceModel.TenantName}__{serviceModel.Index}__{serviceModel.Key}";
        
        return _propertyNameCache.GetOrAdd(cacheKey, static key => GeneratePropertyName(key));
    }
    
    private static string GeneratePropertyName(string cacheKey)
    {
        var parts = cacheKey.Split(new[] { "__" }, StringSplitOptions.None);
        var originalString = parts[0];
        var tenantName = parts[1];
        var index = parts[2];
        var serviceKey = parts.Length > 3 ? parts[3] : null;
        
        StringBuilder sb = new(originalString.Length * 2);
        
        foreach (var c in originalString)
        {
            if (CharReplacements.TryGetValue(c, out var replacement))
            {
                sb.Append(replacement);
            }
            else if (c == '.')
            {
                sb.Append("__");
            }
            else
            {
                sb.Append(c);
            }
        }
        
        var typeString = sb.ToString();
        var propertyName = $"{typeString}__{tenantName}__{index}";

        return !string.IsNullOrEmpty(serviceKey) ? $"Keyed__{propertyName}__{serviceKey}" : propertyName;
    }

    public static string AsField(ServiceModel serviceModel)
    {
        var cacheKey = $"FIELD_{serviceModel.ServiceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}__{serviceModel.TenantName}__{serviceModel.Index}__{serviceModel.Key}";
        
        return _fieldNameCache.GetOrAdd(cacheKey, static key => GenerateFieldName(key));
    }
    
    private static string GenerateFieldName(string cacheKey)
    {
        var propertyName = GeneratePropertyName(cacheKey[6..]); // Remove "FIELD_" prefix
        return $"_{propertyName[..1].ToLower()}{propertyName[1..]}";
    }
}