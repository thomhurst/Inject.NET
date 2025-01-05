using Inject.NET.SourceGenerator.Tests.Options;

namespace Inject.NET.SourceGenerator.Tests;

public class DependenciesSourceGeneratorTests : TestsBase<DependenciesSourceGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "Inject.NET.SourceGenerator.Sample",
            "MyServiceProvider.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Sourcy.Git.RootDirectory.FullName,
                    "Inject.NET.SourceGenerator.Sample",
                    "Models",
                    "Class1.cs"),
                Path.Combine(Sourcy.Git.RootDirectory.FullName,
                    "Inject.NET.SourceGenerator.Sample",
                    "Models",
                    "Class2.cs"),
                Path.Combine(Sourcy.Git.RootDirectory.FullName,
                    "Inject.NET.SourceGenerator.Sample",
                    "Models",
                    "Class3.cs"),
                Path.Combine(Sourcy.Git.RootDirectory.FullName,
                    "Inject.NET.SourceGenerator.Sample",
                    "Models",
                    "Class4.cs"),
                Path.Combine(Sourcy.Git.RootDirectory.FullName,
                    "Inject.NET.SourceGenerator.Sample",
                    "Models",
                    "Class5.cs"),
                Path.Combine(Sourcy.Git.RootDirectory.FullName,
                    "Inject.NET.SourceGenerator.Sample",
                    "Models",
                    "Class6.cs"),
                Path.Combine(Sourcy.Git.RootDirectory.FullName,
                    "Inject.NET.SourceGenerator.Sample",
                    "Models",
                    "IClass.cs"),
            ]
        },
        async generatedFiles => { await Assert.That(generatedFiles.Length).IsEqualTo(2); });
}