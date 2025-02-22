using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Inject.NET.SourceGenerator.Tests;

internal sealed class TestAnalyzerConfigOptionsProvider(
    ImmutableDictionary<string, string>? globalOptions = null,
    ImmutableDictionary<string, ImmutableDictionary<string, string>>? fileOptions = null)
    : AnalyzerConfigOptionsProvider
{
    private readonly ImmutableDictionary<string, string> _globalOptions = globalOptions ?? ImmutableDictionary<string, string>.Empty;
    private readonly ImmutableDictionary<string, ImmutableDictionary<string, string>> _fileOptions = fileOptions ?? ImmutableDictionary<string, ImmutableDictionary<string, string>>.Empty;

    public override AnalyzerConfigOptions GlobalOptions => new SimpleAnalyzerConfigOptions(_globalOptions);

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
    {
        return GetOptions(tree.FilePath);
    }

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
    {
        return GetOptions(textFile.Path);
    }

    private AnalyzerConfigOptions GetOptions(string path)
    {
        if (_fileOptions.TryGetValue(path, out var options))
        {
            return new SimpleAnalyzerConfigOptions(options);
        }
        
        return new SimpleAnalyzerConfigOptions(_globalOptions);
    }
}