namespace Inject.NET.SourceGenerator.Tests;

public class Scoped : TestsBase<DependenciesSourceGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "Inject.NET.Tests",
            "Scoped.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
        });
}