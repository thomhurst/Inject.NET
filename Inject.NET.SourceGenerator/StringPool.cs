using System.Collections.Concurrent;
using System.Text;

namespace Inject.NET.SourceGenerator;

/// <summary>
/// Provides string pooling for commonly used strings in source generation to reduce allocations.
/// </summary>
internal static class StringPool
{
    private static readonly ConcurrentDictionary<string, string> Pool = new();
    
    // Common strings used in code generation
    public static readonly string PublicClass = GetOrAdd("public class ");
    public static readonly string PrivateClass = GetOrAdd("private class ");
    public static readonly string InternalClass = GetOrAdd("internal class ");
    public static readonly string Override = GetOrAdd("override ");
    public static readonly string Public = GetOrAdd("public ");
    public static readonly string Private = GetOrAdd("private ");
    public static readonly string Internal = GetOrAdd("internal ");
    public static readonly string Async = GetOrAdd("async ");
    public static readonly string ValueTask = GetOrAdd("ValueTask");
    public static readonly string Return = GetOrAdd("return ");
    public static readonly string Await = GetOrAdd("await ");
    public static readonly string New = GetOrAdd("new ");
    public static readonly string This = GetOrAdd("this");
    public static readonly string Null = GetOrAdd("null");
    public static readonly string True = GetOrAdd("true");
    public static readonly string False = GetOrAdd("false");
    public static readonly string Var = GetOrAdd("var ");
    public static readonly string If = GetOrAdd("if ");
    public static readonly string Else = GetOrAdd("else");
    public static readonly string Get = GetOrAdd("get");
    public static readonly string Set = GetOrAdd("set");
    public static readonly string OpenBrace = GetOrAdd("{");
    public static readonly string CloseBrace = GetOrAdd("}");
    public static readonly string OpenParen = GetOrAdd("(");
    public static readonly string CloseParen = GetOrAdd(")");
    public static readonly string Semicolon = GetOrAdd(";");
    public static readonly string Arrow = GetOrAdd(" => ");
    public static readonly string Dot = GetOrAdd(".");
    public static readonly string Comma = GetOrAdd(", ");
    public static readonly string CommaSpace = GetOrAdd(", ");
    public static readonly string Question = GetOrAdd("?");
    public static readonly string Exclamation = GetOrAdd("!");
    public static readonly string Underscore = GetOrAdd("_");
    public static readonly string ServiceFactories = GetOrAdd("serviceFactories");
    public static readonly string ServiceProvider = GetOrAdd("ServiceProvider_");
    public static readonly string ServiceScope = GetOrAdd("ServiceScope_");
    public static readonly string SingletonScope = GetOrAdd("SingletonScope_");
    public static readonly string ServiceRegistrar = GetOrAdd("ServiceRegistrar_");
    public static readonly string GlobalInject = GetOrAdd("global::Inject.NET.");
    public static readonly string Services = GetOrAdd("Services.");
    public static readonly string Models = GetOrAdd("Models.");
    public static readonly string Interfaces = GetOrAdd("Interfaces.");
    public static readonly string InitializeAsync = GetOrAdd("InitializeAsync");
    public static readonly string CreateTypedScope = GetOrAdd("CreateTypedScope");
    public static readonly string Singletons = GetOrAdd("Singletons");
    public static readonly string Tenant = GetOrAdd("Tenant_");
    
    /// <summary>
    /// Gets an interned string from the pool, or adds it if not present.
    /// </summary>
    /// <param name="value">The string to intern</param>
    /// <returns>The interned string</returns>
    public static string GetOrAdd(string value)
    {
        return Pool.GetOrAdd(value, static v => v);
    }
    
    /// <summary>
    /// Combines multiple strings efficiently using the string pool.
    /// </summary>
    /// <param name="parts">The string parts to combine</param>
    /// <returns>The combined string</returns>
    public static string Combine(params string[] parts)
    {
        return parts.Length switch
        {
            0 => string.Empty,
            1 => GetOrAdd(parts[0]),
            _ => CombineMultiple(parts)
        };
    }

    private static string CombineMultiple(string[] parts)
    {
        
        var totalLength = 0;
        foreach (var part in parts)
        {
            totalLength += part.Length;
        }
        
        // Use StringBuilder for .NET Standard compatibility
        StringBuilder sb = new(totalLength);
        foreach (var part in parts)
        {
            sb.Append(part);
        }
        return GetOrAdd(sb.ToString());
    }
    
    /// <summary>
    /// Builds a property name by combining prefix and suffix with proper casing.
    /// </summary>
    /// <param name="prefix">The prefix part</param>
    /// <param name="suffix">The suffix part</param>
    /// <returns>The combined property name</returns>
    public static string BuildPropertyName(string prefix, string suffix)
    {
        if (string.IsNullOrEmpty(prefix)) return GetOrAdd(suffix);
        if (string.IsNullOrEmpty(suffix)) return GetOrAdd(prefix);
        
        return GetOrAdd(prefix + suffix);
    }
}