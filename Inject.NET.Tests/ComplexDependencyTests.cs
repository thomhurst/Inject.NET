
using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class ComplexDependencyTests
{
    [Test]
    public async Task CanResolve_ComplexDependencyGraph()
    {
        var serviceProvider = await ComplexDependencyServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var root = scope.GetRequiredService<Root>();

        await Assert.That(root).IsNotNull();
        await Assert.That(root.Child1).IsNotNull();
        await Assert.That(root.Child2).IsNotNull();
        await Assert.That(root.Child1.GrandChild).IsNotNull();
        await Assert.That(root.Child2.GrandChild).IsNotNull();
        await Assert.That(root.Child1.GrandChild).IsEqualTo(root.Child2.GrandChild);
    }

    [ServiceProvider]
    [Scoped<Root>]
    [Scoped<Child1>]
    [Scoped<Child2>]
    [Scoped<GrandChild>]
    public partial class ComplexDependencyServiceProvider;

    public class Root
    {
        public Root(Child1 child1, Child2 child2)
        {
            Child1 = child1;
            Child2 = child2;
        }

        public Child1 Child1 { get; }
        public Child2 Child2 { get; }
    }

    public class Child1
    {
        public Child1(GrandChild grandChild)
        {
            GrandChild = grandChild;
        }

        public GrandChild GrandChild { get; }
    }

    public class Child2
    {
        public Child2(GrandChild grandChild)
        {
            GrandChild = grandChild;
        }

        public GrandChild GrandChild { get; }
    }

    public class GrandChild;
}