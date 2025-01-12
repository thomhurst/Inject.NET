namespace Inject.NET.SourceGenerator.Tests;

public class Transient : TestsBase<DependenciesSourceGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "Inject.NET.Tests",
            "Transient.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(4);
        });
}