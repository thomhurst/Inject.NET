using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class NullableDependencyTests
{
    [Test]
    public async Task CanResolve_NullableDependency()
    {
        var serviceProvider = await NullableDependencyServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var dependent = scope.GetRequiredService<Dependent>();

        await Assert.That(dependent).IsNotNull();
        await Assert.That(dependent.OptionalDependency).IsNull();
    }

    [ServiceProvider]
    [Scoped<Dependent>]
    public partial class NullableDependencyServiceProvider;

    public class Dependent
    {
        public Dependent(OptionalDependency? optionalDependency)
        {
            OptionalDependency = optionalDependency;
        }

        public OptionalDependency? OptionalDependency { get; }
    }

    public class OptionalDependency;
}