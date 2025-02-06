﻿using Inject.NET.SourceGenerator.Tests.Options;

namespace Inject.NET.SourceGenerator.Tests;

public class CircularDependencyTests : TestsBase<DependenciesSourceGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "Inject.NET.Tests",
            "CircularDependencyTests.cs"),
        new RunTestOptions
        {
            ExpectedErrors = ["error IJN0001: Conflict: Inject.NET.Tests.CircularDependencyTests.Class1 depends on Inject.NET.Tests.CircularDependencyTests.Class2 which depends on Inject.NET.Tests.CircularDependencyTests.Class1"],
        },
        async generatedFiles => { await Assert.That(generatedFiles.Length).IsEqualTo(0); });
}