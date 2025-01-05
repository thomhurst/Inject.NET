using Inject.NET.SourceGenerator.Tests.Options;

namespace Inject.NET.SourceGenerator.Tests;

public class WithTenantTests : TestsBase<DependenciesSourceGenerator>
{
    [Test]
    public Task WithTenant() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "Inject.NET.SourceGenerator.Sample",
            "ServiceProviders",
            $"{TestContext.Current!.TestDetails.TestName}.cs"),
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
                    "InheritsFromClass1.cs"),
                Path.Combine(Sourcy.Git.RootDirectory.FullName,
                    "Inject.NET.SourceGenerator.Sample",
                    "Models",
                    "IClass.cs"),
            ]
        },
        async generatedFiles => { await Assert.That(generatedFiles.Length).IsEqualTo(2); });
}