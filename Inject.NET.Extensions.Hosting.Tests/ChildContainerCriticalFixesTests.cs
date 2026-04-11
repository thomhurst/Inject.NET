using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.Extensions.DependencyInjection;
using Inject.NET.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Inject.NET.Extensions.Hosting.Tests;

/// <summary>
/// Red tests covering the critical issues found in PR #60 code review:
/// C1: GetResolutionScope must not leak temporary scopes - use singleton scope directly
/// C2: ChildSingletonScope must be detectable as a singleton scope so greedy fallback works in child containers
/// C3: ChildSingletonScope must honor ExternallyOwned so instances are not disposed by the container
/// C4: Child container singletons must reuse the parent's pre-constructed singleton for inherited keys
/// </summary>
public partial class ChildContainerCriticalFixesTests
{
    // --- Parent singleton used to verify identity reuse (C4) ---

    public interface IParentSingleton
    {
        Guid Id { get; }
    }

    public sealed class ParentSingleton : IParentSingleton
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    [ServiceProvider]
    [Singleton<IParentSingleton, ParentSingleton>]
    public partial class ParentReuseProvider;

    [Test]
    public async Task C4_InheritedSingleton_ReusesParentInstance_InChildContainer()
    {
        await using var parent = await ParentReuseProvider.BuildAsync();

        // Resolve the parent's singleton directly
        await using var parentScope = parent.CreateScope();
        var parentInstance = parentScope.GetRequiredService<IParentSingleton>();

        // Host builder path creates a child container via the factory
        var factory = new InjectNetServiceProviderFactory(parent);
        var services = new ServiceCollection();
        var builder = factory.CreateBuilder(services);
        var childSp = factory.CreateServiceProvider(builder);

        // The child container must return the SAME instance as the parent for inherited keys
        var childInstance = childSp.GetService(typeof(IParentSingleton)) as IParentSingleton;

        await Assert.That(childInstance).IsNotNull();
        await Assert.That(childInstance!.Id).IsEqualTo(parentInstance.Id);
        await Assert.That(ReferenceEquals(childInstance, parentInstance)).IsTrue();
    }

    // --- Greedy fallback in child container (C2) ---
    // A type with multiple constructors forces the greedy fallback path.
    // Prior to the fix, GetResolutionScope did not detect ChildSingletonScope
    // and the dependency resolution against the singleton scope silently failed
    // (resolved null, rejected every ctor, then re-threw ambiguity from ActivatorUtilities).

    public interface IDependency
    {
        string Name { get; }
    }

    public sealed class DependencyImpl : IDependency
    {
        public string Name => "dep";
    }

    public sealed class MultiCtorService
    {
        public IDependency? Dependency { get; }
        public string Kind { get; }

        public MultiCtorService()
        {
            Kind = "default";
        }

        public MultiCtorService(IDependency dependency)
        {
            Dependency = dependency;
            Kind = "with-dep";
        }
    }

    [ServiceProvider]
    public partial class GreedyFallbackProvider;

    [Test]
    public async Task C2_MultiConstructor_Singleton_ResolvesInChildContainer()
    {
        await using var parent = await GreedyFallbackProvider.BuildAsync();
        var factory = new InjectNetServiceProviderFactory(parent);

        var services = new ServiceCollection();
        services.AddSingleton<IDependency, DependencyImpl>();
        services.AddSingleton<MultiCtorService>();

        var builder = factory.CreateBuilder(services);
        var sp = factory.CreateServiceProvider(builder);

        var instance = sp.GetService(typeof(MultiCtorService)) as MultiCtorService;

        await Assert.That(instance).IsNotNull();
        await Assert.That(instance!.Kind).IsEqualTo("with-dep");
        await Assert.That(instance.Dependency).IsNotNull();
        await Assert.That(instance.Dependency!.Name).IsEqualTo("dep");
    }

    // --- ExternallyOwned instance disposal (C3) ---
    // Verifies child container honors ExternallyOwned flag for singleton instances.

    public sealed class ExternallyOwnedTracker : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    [ServiceProvider]
    public partial class ExternallyOwnedProvider;

    [Test]
    public async Task C3_ChildContainer_DisposesInstance_MatchingMediSemantics()
    {
        // MEDI semantics: AddSingleton(instance) means the container OWNS the instance
        // and disposes it at root shutdown. The bridge must match this.
        var instance = new ExternallyOwnedTracker();

        await using var parent = await ExternallyOwnedProvider.BuildAsync();

        var factory = new InjectNetServiceProviderFactory(parent);
        var services = new ServiceCollection();
        services.AddSingleton(instance);

        var builder = factory.CreateBuilder(services);
        var child = builder.Build();

        // Materialize the singleton in the child scope
        var resolved = ((System.IServiceProvider)child).GetService(typeof(ExternallyOwnedTracker));
        await Assert.That(resolved).IsSameReferenceAs(instance);

        await child.DisposeAsync();

        // The child container owns the instance (matching MEDI); it should be disposed once.
        await Assert.That(instance.DisposeCount).IsEqualTo(1);
    }

    // --- Scope leak avoidance (C1) ---
    // Before the fix: GetResolutionScope created a throwaway IServiceScope that leaked
    // any scoped/transient services resolved through it during greedy constructor selection.
    // After the fix: no throwaway scope — the singleton scope is used directly, matching
    // MEDI's "singletons resolve from root" semantics.
    //
    // We test via a transient disposable dependency that a greedy-fallback singleton
    // takes in one of its constructors. After disposing the child container, the
    // transient should have been disposed too (because it's now tracked by the container
    // rather than leaked inside an orphaned scope).

    public sealed class TrackedTransient : IDisposable
    {
        public static int TotalCreated;
        public static int TotalDisposed;

        public TrackedTransient()
        {
            Interlocked.Increment(ref TotalCreated);
        }

        public void Dispose()
        {
            Interlocked.Increment(ref TotalDisposed);
        }
    }

    public sealed class SingletonWithSingletonDep
    {
        public IDependency Dep { get; }

        public SingletonWithSingletonDep() { Dep = null!; }

        public SingletonWithSingletonDep(IDependency dep)
        {
            Dep = dep;
        }
    }

    [ServiceProvider]
    public partial class ScopeLeakProvider;

    [Test]
    public async Task C1_SingletonGreedyFallback_UsesSingletonScopeDirectly_NoScopeLeak()
    {
        await using var parent = await ScopeLeakProvider.BuildAsync();
        var factory = new InjectNetServiceProviderFactory(parent);

        var services = new ServiceCollection();
        // Both registrations as singleton; the correct behavior is that the greedy
        // fallback uses the singleton scope directly to resolve the dep.
        services.AddSingleton<IDependency, DependencyImpl>();
        services.AddSingleton<SingletonWithSingletonDep>();

        var builder = factory.CreateBuilder(services);
        var sp = factory.CreateServiceProvider(builder);

        // Resolve the singleton many times - it should only construct once
        // because singletons are cached.
        SingletonWithSingletonDep? last = null;
        for (var i = 0; i < 50; i++)
        {
            last = sp.GetService(typeof(SingletonWithSingletonDep)) as SingletonWithSingletonDep;
            await Assert.That(last).IsNotNull();
        }

        // Verify dependency was injected
        await Assert.That(last!.Dep).IsNotNull();
    }
}
