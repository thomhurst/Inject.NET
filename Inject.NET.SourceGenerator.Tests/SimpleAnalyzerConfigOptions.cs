using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Inject.NET.SourceGenerator.Tests;

internal sealed class SimpleAnalyzerConfigOptions(ImmutableDictionary<string, string> options) : AnalyzerConfigOptions
{
    public override bool TryGetValue(string key, out string value)
    {
        return options.TryGetValue(key, out value!);
    }
}