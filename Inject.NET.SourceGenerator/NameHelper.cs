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
    private static readonly HashSet<string> _qualifiedTypes = new();
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

    private static readonly SymbolDisplayFormat QualifiedTypeNameFormat = new SymbolDisplayFormat(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    /// <summary>
    /// Detects service types whose short names collide and marks them for namespace-qualified naming.
    /// Must be called before any property names are generated for a provider.
    /// </summary>
    public static void PrepareForProvider(IEnumerable<ServiceModel> allServiceModels)
    {
        _propertyNameCache.Clear();
        _fieldNameCache.Clear();
        _qualifiedTypes.Clear();

        var typesByShortName = new Dictionary<string, List<string>>();

        foreach (var model in allServiceModels)
        {
            if (model.IsOpenGeneric)
            {
                continue;
            }

            var shortName = model.ServiceType.ToDisplayString(ShortTypeNameFormat);
            var fullName = model.ServiceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

            if (!typesByShortName.TryGetValue(shortName, out var fullNames))
            {
                fullNames = new List<string>();
                typesByShortName[shortName] = fullNames;
            }

            if (!fullNames.Contains(fullName))
            {
                fullNames.Add(fullName);
            }
        }

        foreach (var (_, fullNames) in typesByShortName)
        {
            if (fullNames.Count > 1)
            {
                foreach (var fullName in fullNames)
                {
                    _qualifiedTypes.Add(fullName);
                }
            }
        }
    }

    public static string AsProperty(ServiceModel serviceModel)
    {
        var cacheKey = $"{serviceModel.ServiceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}|{serviceModel.TenantName}|{serviceModel.Index}|{serviceModel.Key}";

        return _propertyNameCache.GetOrAdd(cacheKey, _ =>
        {
            var fullName = serviceModel.ServiceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            var displayName = _qualifiedTypes.Contains(fullName)
                ? serviceModel.ServiceType.ToDisplayString(QualifiedTypeNameFormat)
                : serviceModel.ServiceType.ToDisplayString(ShortTypeNameFormat);
            return GeneratePropertyName(displayName, serviceModel.TenantName, serviceModel.Index.ToString(), serviceModel.Key);
        });
    }

    private static string GeneratePropertyName(string shortTypeName, string? tenantName, string index, string? serviceKey)
    {
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