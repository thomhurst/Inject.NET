using Inject.NET.SourceGenerator.Tests.Options;

namespace Inject.NET.SourceGenerator.Tests;

public class DependenciesSourceGeneratorTests : TestsBase<DependenciesSourceGenerator>
{
    [Test]
    public Task SingletonImplementationOnly() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
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
                    "IClass.cs"),
            ]
        },
        async generatedFiles => { await Assert.That(generatedFiles.Length).IsEqualTo(2); });
    
    [Test]
    public Task SingletonServiceImplementation() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
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
                    "IClass.cs"),
            ]
        },
        async generatedFiles => { await Assert.That(generatedFiles.Length).IsEqualTo(2); });
    
    [Test]
    public Task ScopedImplementationOnly() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
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
                    "IClass.cs"),
            ]
        },
        async generatedFiles => { await Assert.That(generatedFiles.Length).IsEqualTo(2); });
    
    [Test]
    public Task ScopedServiceImplementation() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
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
                    "IClass.cs"),
            ]
        },
        async generatedFiles => { await Assert.That(generatedFiles.Length).IsEqualTo(2); });
    
    [Test]
    public Task TransientImplementationOnly() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
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
                    "IClass.cs"),
            ]
        },
        async generatedFiles => { await Assert.That(generatedFiles.Length).IsEqualTo(2); });
    
    [Test]
    public Task TransientServiceImplementation() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
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
                    "IClass.cs"),
            ]
        },
        async generatedFiles => { await Assert.That(generatedFiles.Length).IsEqualTo(2); });
}