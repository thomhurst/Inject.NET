using System.Diagnostics.CodeAnalysis;
using Inject.NET.Interfaces;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Tests;

[SuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file")]
internal class ReferencesHelper
{
    public static readonly List<PortableExecutableReference> References =
        AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location))
            .Select(x => MetadataReference.CreateFromFile(x.Location))
            .Concat([
                // add your app/lib specifics, e.g.:
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IServiceProvider<>).Assembly.Location),
            ])
            .ToList();
}