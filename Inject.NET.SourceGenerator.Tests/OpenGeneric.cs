namespace Inject.NET.SourceGenerator.Tests;

public class OpenGenericTests : TestsBase<DependenciesSourceGenerator>
{
    [Test]
    public Task OpenGeneric() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "Inject.NET.SourceGenerator.Sample",
            "ServiceProviders",
            $"{TestContext.Current!.TestDetails.TestName}.cs"),
        async generatedFiles => { await Assert.That(generatedFiles.Length).IsEqualTo(2); });
}