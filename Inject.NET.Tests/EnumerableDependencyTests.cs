
using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class EnumerableDependencyTests
{
    [Test]
    public async Task CanResolve_Type_With_EnumerableDependency()
    {
        var serviceProvider = await EnumerableDependencyServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var dependent = scope.GetRequiredService<Dependent>();

        await Assert.That(dependent).IsNotNull();
        await Assert.That(dependent.Dependencies).HasCount().EqualTo(3);
    }
    
    [Test]
    public async Task CanResolve_Type_With_ReadOnlyListDependency()
    {
        var serviceProvider = await EnumerableDependencyServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var dependent = scope.GetRequiredService<Dependent2>();

        await Assert.That(dependent).IsNotNull();
        await Assert.That(dependent.Dependencies).HasCount().EqualTo(3);
    }
    
    [Test]
    public async Task CanResolve_EnumerableDependency_Directly()
    {
        var serviceProvider = await EnumerableDependencyServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var dependent = scope.GetServices<IDependency>();

        await Assert.That(dependent).HasCount().EqualTo(3);
    }

    [ServiceProvider]
    [Scoped<Dependent>]
    [Scoped<Dependent2>]
    [Scoped<IDependency, Dependency1>]
    [Scoped<IDependency, Dependency2>]
    [Scoped<IDependency, Dependency3>]
    public partial class EnumerableDependencyServiceProvider;

    public class Dependent
    {
        public Dependent(IEnumerable<IDependency> dependencies)
        {
            Dependencies = dependencies.ToList();
        }

        public List<IDependency> Dependencies { get; }
    }
    
    public class Dependent2
    {
        public Dependent2(IReadOnlyList<IDependency> dependencies)
        {
            Dependencies = dependencies.ToList();
        }

        public List<IDependency> Dependencies { get; }
    }

    public interface IDependency;

    public class Dependency1 : IDependency;
    public class Dependency2 : IDependency;
    public class Dependency3 : IDependency;
}