
using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class OptionalDependencyTests
{
    [Test]
    public async Task CanResolve_OptionalDependency()
    {
        var serviceProvider = await OptionalDependencyServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var dependent = scope.GetRequiredService<Dependent>();

        await Assert.That(dependent).IsNotNull();
        await Assert.That(dependent.OptionalDependency).IsNull();
    }

    [ServiceProvider]
    [Scoped<Dependent>]
    public partial class OptionalDependencyServiceProvider;

    public class Dependent
    {
        public Dependent(OptionalDependency? optionalDependency = null)
        {
            OptionalDependency = optionalDependency;
        }

        public OptionalDependency? OptionalDependency { get; }
    }

    public class OptionalDependency;
}