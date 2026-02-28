using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.Services;

namespace Inject.NET.Tests;

/// <summary>
/// Tests for child/nested container support.
/// Child containers inherit all registrations from their parent but can add or override services.
/// Parent containers are unaffected by child registrations.
/// </summary>
public partial class ChildContainerTests
{
    [Test]
    public async Task Child_InheritsParentRegistrations()
    {
        await using var serviceProvider = await ChildContainerServiceProvider.BuildAsync();

        await using var child = serviceProvider.CreateChildContainer(registrar => { });

        await using var scope = child.CreateScope();
        var service = scope.GetRequiredService<IParentService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service.Name).IsEqualTo("Parent");
    }

    [Test]
    public async Task Child_CanOverrideParentRegistrations()
    {
        await using var serviceProvider = await ChildContainerServiceProvider.BuildAsync();

        await using var child = serviceProvider.CreateChildContainer(registrar =>
        {
            registrar.AddSingleton<IParentService>(new ParentServiceImpl("ChildOverride"));
        });

        await using var scope = child.CreateScope();
        var service = scope.GetRequiredService<IParentService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service.Name).IsEqualTo("ChildOverride");
    }

    [Test]
    public async Task Child_CanAddNewRegistrations()
    {
        await using var serviceProvider = await ChildContainerServiceProvider.BuildAsync();

        await using var child = serviceProvider.CreateChildContainer(registrar =>
        {
            registrar.AddSingleton<IChildOnlyService>(new ChildOnlyServiceImpl("FromChild"));
        });

        await using var scope = child.CreateScope();
        var service = scope.GetRequiredService<IChildOnlyService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service.Value).IsEqualTo("FromChild");
    }

    [Test]
    public async Task Parent_DoesNotSeeChildRegistrations()
    {
        await using var serviceProvider = await ChildContainerServiceProvider.BuildAsync();

        await using var child = serviceProvider.CreateChildContainer(registrar =>
        {
            registrar.AddSingleton<IChildOnlyService>(new ChildOnlyServiceImpl("FromChild"));
        });

        await using var parentScope = serviceProvider.CreateScope();
        var service = parentScope.GetOptionalService<IChildOnlyService>();

        await Assert.That(service).IsNull();
    }

    [Test]
    public async Task DisposingChild_DoesNotAffectParent()
    {
        await using var serviceProvider = await ChildContainerServiceProvider.BuildAsync();

        var child = serviceProvider.CreateChildContainer(registrar => { });

        await using var childScope = child.CreateScope();
        var childService = childScope.GetRequiredService<IParentService>();
        await Assert.That(childService).IsNotNull();

        // Dispose the child scope and child container
        await childScope.DisposeAsync();
        await child.DisposeAsync();

        // Parent should still work fine
        await using var parentScope = serviceProvider.CreateScope();
        var parentService = parentScope.GetRequiredService<IParentService>();

        await Assert.That(parentService).IsNotNull();
        await Assert.That(parentService.Name).IsEqualTo("Parent");
    }

    [Test]
    public async Task Child_OverriddenSingleton_IsIndependentFromParent()
    {
        await using var serviceProvider = await ChildContainerServiceProvider.BuildAsync();

        await using var child = serviceProvider.CreateChildContainer(registrar =>
        {
            registrar.AddSingleton<IParentService>(new ParentServiceImpl("ChildSingleton"));
        });

        // Resolve from parent
        await using var parentScope = serviceProvider.CreateScope();
        var parentService = parentScope.GetRequiredService<IParentService>();

        // Resolve from child
        await using var childScope = child.CreateScope();
        var childService = childScope.GetRequiredService<IParentService>();

        await Assert.That(parentService.Name).IsEqualTo("Parent");
        await Assert.That(childService.Name).IsEqualTo("ChildSingleton");
        await Assert.That(parentService).IsNotSameReferenceAs(childService);
    }

    [Test]
    public async Task Child_InheritedSingleton_IsSameInstanceAcrossChildScopes()
    {
        await using var serviceProvider = await ChildContainerServiceProvider.BuildAsync();

        await using var child = serviceProvider.CreateChildContainer(registrar => { });

        await using var scope1 = child.CreateScope();
        await using var scope2 = child.CreateScope();

        var service1 = scope1.GetRequiredService<IParentService>();
        var service2 = scope2.GetRequiredService<IParentService>();

        await Assert.That(service1).IsNotNull();
        await Assert.That(service2).IsNotNull();
        // Both resolve to the same singleton from the child's singleton scope
        await Assert.That(service1).IsSameReferenceAs(service2);
    }

    [Test]
    public async Task NestedChild_InheritsFromChild()
    {
        await using var serviceProvider = await ChildContainerServiceProvider.BuildAsync();

        await using var child = serviceProvider.CreateChildContainer(registrar =>
        {
            registrar.AddSingleton<IChildOnlyService>(new ChildOnlyServiceImpl("FromChild"));
        });

        await using var grandchild = child.CreateChildContainer(registrar => { });

        await using var scope = grandchild.CreateScope();

        // Grandchild should see parent's registration
        var parentService = scope.GetRequiredService<IParentService>();
        await Assert.That(parentService.Name).IsEqualTo("Parent");

        // Grandchild should see child's registration
        var childService = scope.GetRequiredService<IChildOnlyService>();
        await Assert.That(childService.Value).IsEqualTo("FromChild");
    }

    [Test]
    public async Task NestedChild_CanOverrideChildRegistrations()
    {
        await using var serviceProvider = await ChildContainerServiceProvider.BuildAsync();

        await using var child = serviceProvider.CreateChildContainer(registrar =>
        {
            registrar.AddSingleton<IChildOnlyService>(new ChildOnlyServiceImpl("FromChild"));
        });

        await using var grandchild = child.CreateChildContainer(registrar =>
        {
            registrar.AddSingleton<IChildOnlyService>(new ChildOnlyServiceImpl("FromGrandchild"));
        });

        await using var childScope = child.CreateScope();
        var childService = childScope.GetRequiredService<IChildOnlyService>();
        await Assert.That(childService.Value).IsEqualTo("FromChild");

        await using var grandchildScope = grandchild.CreateScope();
        var grandchildService = grandchildScope.GetRequiredService<IChildOnlyService>();
        await Assert.That(grandchildService.Value).IsEqualTo("FromGrandchild");
    }

    [Test]
    public async Task Child_ScopedServices_AreCreatedFreshPerScope()
    {
        await using var serviceProvider = await ChildContainerServiceProvider.BuildAsync();

        await using var child = serviceProvider.CreateChildContainer(registrar =>
        {
            registrar.AddScoped<IScopedCounter, ScopedCounter>();
        });

        await using var scope1 = child.CreateScope();
        await using var scope2 = child.CreateScope();

        var counter1 = scope1.GetRequiredService<IScopedCounter>();
        var counter2 = scope2.GetRequiredService<IScopedCounter>();

        await Assert.That(counter1).IsNotSameReferenceAs(counter2);
    }

    [Test]
    public async Task Child_TransientServices_AreCreatedFreshEachTime()
    {
        await using var serviceProvider = await ChildContainerServiceProvider.BuildAsync();

        await using var child = serviceProvider.CreateChildContainer(registrar =>
        {
            registrar.AddTransient<IScopedCounter, ScopedCounter>();
        });

        await using var scope = child.CreateScope();

        var counter1 = scope.GetRequiredService<IScopedCounter>();
        var counter2 = scope.GetRequiredService<IScopedCounter>();

        await Assert.That(counter1).IsNotSameReferenceAs(counter2);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Service Provider Definition
    // ═══════════════════════════════════════════════════════════════════════

    [ServiceProvider]
    public partial class ChildContainerServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<IParentService>(new ParentServiceImpl("Parent"));
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Test Services
    // ═══════════════════════════════════════════════════════════════════════

    public interface IParentService
    {
        string Name { get; }
    }

    public class ParentServiceImpl(string name) : IParentService
    {
        public string Name => name;
    }

    public interface IChildOnlyService
    {
        string Value { get; }
    }

    public class ChildOnlyServiceImpl(string value) : IChildOnlyService
    {
        public string Value => value;
    }

    public interface IScopedCounter { }

    public class ScopedCounter : IScopedCounter { }
}
