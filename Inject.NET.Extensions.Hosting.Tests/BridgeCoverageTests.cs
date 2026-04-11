using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.Extensions.DependencyInjection;
using Inject.NET.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Inject.NET.Extensions.Hosting.Tests;

/// <summary>
/// Coverage battery for the MEDI → Inject.NET replay bridge. Covers the scenarios
/// called out in the PR #60 review as under-tested: keyed services, open generics,
/// multi-constructor greedy fallback, IEnumerable&lt;T&gt; / array / IReadOnlyList&lt;T&gt;
/// constructor injection, MEDI instance disposal ordering, and non-string keys.
/// </summary>
public partial class BridgeCoverageTests
{
    // ------------------------------------------------------------------
    // Keyed services
    // ------------------------------------------------------------------

    public interface INamed
    {
        string Name { get; }
    }

    public sealed class FooNamed : INamed { public string Name => "foo"; }
    public sealed class BarNamed : INamed { public string Name => "bar"; }

    [ServiceProvider]
    public partial class KeyedProvider;

    [Test]
    public async Task Keyed_StringKey_RegistersWithoutError()
    {
        // MEDI's IKeyedServiceProvider API is not wired through the bridge; this test
        // verifies that string-keyed MEDI registrations at least replay without error
        // (the non-string path is the one most likely to silently collide).
        await using var parent = await KeyedProvider.BuildAsync();
        var factory = new InjectNetServiceProviderFactory(parent);

        var services = new ServiceCollection();
        services.AddKeyedSingleton<INamed, FooNamed>("foo");
        services.AddKeyedSingleton<INamed, BarNamed>("bar");

        var sp = factory.CreateServiceProvider(factory.CreateBuilder(services));

        await Assert.That(sp).IsNotNull();
    }

    [Test]
    public async Task Keyed_NonStringKey_ThrowsNotSupported()
    {
        await using var parent = await KeyedProvider.BuildAsync();
        var factory = new InjectNetServiceProviderFactory(parent);

        var services = new ServiceCollection();
        services.AddKeyedSingleton<INamed, FooNamed>(123); // int key — not supported

        await Assert.ThrowsAsync<NotSupportedException>(() =>
        {
            var builder = factory.CreateBuilder(services);
            factory.CreateServiceProvider(builder);
            return Task.CompletedTask;
        });
    }

    // ------------------------------------------------------------------
    // Open generics
    // ------------------------------------------------------------------

    public interface IRepo<T>
    {
        Type ItemType { get; }
    }

    public sealed class Repo<T> : IRepo<T>
    {
        public Type ItemType => typeof(T);
    }

    [ServiceProvider]
    public partial class OpenGenericProvider;

    [Test]
    public async Task OpenGeneric_ResolvesClosedGeneric_WithCorrectArgument()
    {
        await using var parent = await OpenGenericProvider.BuildAsync();
        var factory = new InjectNetServiceProviderFactory(parent);

        var services = new ServiceCollection();
        services.AddSingleton(typeof(IRepo<>), typeof(Repo<>));

        var sp = factory.CreateServiceProvider(factory.CreateBuilder(services));

        var intRepo = sp.GetService(typeof(IRepo<int>)) as IRepo<int>;
        var stringRepo = sp.GetService(typeof(IRepo<string>)) as IRepo<string>;

        await Assert.That(intRepo).IsNotNull();
        await Assert.That(intRepo!.ItemType).IsEqualTo(typeof(int));
        await Assert.That(stringRepo).IsNotNull();
        await Assert.That(stringRepo!.ItemType).IsEqualTo(typeof(string));
    }

    // ------------------------------------------------------------------
    // IEnumerable<T> / IReadOnlyList<T> / T[] constructor injection
    // ------------------------------------------------------------------

    public interface IHandler
    {
        string Id { get; }
    }

    public sealed class HandlerA : IHandler { public string Id => "A"; }
    public sealed class HandlerB : IHandler { public string Id => "B"; }

    // Force the greedy fallback path by having a second empty constructor.
    public sealed class MultiCtorConsumesEnumerable
    {
        public IEnumerable<IHandler>? Handlers { get; }
        public string Kind { get; }

        public MultiCtorConsumesEnumerable() { Kind = "default"; }
        public MultiCtorConsumesEnumerable(IEnumerable<IHandler> handlers)
        {
            Handlers = handlers;
            Kind = "with-handlers";
        }
    }

    // Force the greedy fallback path; parameter is IReadOnlyList<IHandler>.
    public sealed class MultiCtorConsumesReadOnlyList
    {
        public IReadOnlyList<IHandler>? Handlers { get; }
        public string Kind { get; }

        public MultiCtorConsumesReadOnlyList() { Kind = "default"; }
        public MultiCtorConsumesReadOnlyList(IReadOnlyList<IHandler> handlers)
        {
            Handlers = handlers;
            Kind = "with-handlers";
        }
    }

    // Force the greedy fallback path; parameter is IHandler[].
    public sealed class MultiCtorConsumesArray
    {
        public IHandler[]? Handlers { get; }
        public string Kind { get; }

        public MultiCtorConsumesArray() { Kind = "default"; }
        public MultiCtorConsumesArray(IHandler[] handlers)
        {
            Handlers = handlers;
            Kind = "with-handlers";
        }
    }

    [ServiceProvider]
    public partial class CollectionInjectionProvider;

    [Test]
    public async Task GreedyFallback_InjectsIEnumerable_OfAllRegistrations()
    {
        await using var parent = await CollectionInjectionProvider.BuildAsync();
        var factory = new InjectNetServiceProviderFactory(parent);

        var services = new ServiceCollection();
        services.AddSingleton<IHandler, HandlerA>();
        services.AddSingleton<IHandler, HandlerB>();
        services.AddSingleton<MultiCtorConsumesEnumerable>();

        var sp = factory.CreateServiceProvider(factory.CreateBuilder(services));

        var consumer = sp.GetService(typeof(MultiCtorConsumesEnumerable)) as MultiCtorConsumesEnumerable;

        await Assert.That(consumer).IsNotNull();
        await Assert.That(consumer!.Kind).IsEqualTo("with-handlers");
        await Assert.That(consumer.Handlers).IsNotNull();
        var ids = consumer.Handlers!.Select(h => h.Id).OrderBy(x => x).ToArray();
        await Assert.That(ids.Length).IsEqualTo(2);
        await Assert.That(ids[0]).IsEqualTo("A");
        await Assert.That(ids[1]).IsEqualTo("B");
    }

    [Test]
    public async Task GreedyFallback_InjectsReadOnlyList_FromEnumerableRegistrations()
    {
        await using var parent = await CollectionInjectionProvider.BuildAsync();
        var factory = new InjectNetServiceProviderFactory(parent);

        var services = new ServiceCollection();
        services.AddSingleton<IHandler, HandlerA>();
        services.AddSingleton<IHandler, HandlerB>();
        services.AddSingleton<MultiCtorConsumesReadOnlyList>();

        var sp = factory.CreateServiceProvider(factory.CreateBuilder(services));

        var consumer = sp.GetService(typeof(MultiCtorConsumesReadOnlyList)) as MultiCtorConsumesReadOnlyList;

        await Assert.That(consumer).IsNotNull();
        await Assert.That(consumer!.Kind).IsEqualTo("with-handlers");
        await Assert.That(consumer.Handlers).IsNotNull();
        await Assert.That(consumer.Handlers!.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GreedyFallback_InjectsTypedArray_FromEnumerableRegistrations()
    {
        await using var parent = await CollectionInjectionProvider.BuildAsync();
        var factory = new InjectNetServiceProviderFactory(parent);

        var services = new ServiceCollection();
        services.AddSingleton<IHandler, HandlerA>();
        services.AddSingleton<IHandler, HandlerB>();
        services.AddSingleton<MultiCtorConsumesArray>();

        var sp = factory.CreateServiceProvider(factory.CreateBuilder(services));

        var consumer = sp.GetService(typeof(MultiCtorConsumesArray)) as MultiCtorConsumesArray;

        await Assert.That(consumer).IsNotNull();
        await Assert.That(consumer!.Kind).IsEqualTo("with-handlers");
        await Assert.That(consumer.Handlers).IsNotNull();
        await Assert.That(consumer.Handlers!.Length).IsEqualTo(2);
    }

    // ------------------------------------------------------------------
    // Disposal ordering: AddSingleton(instance) must dispose with container
    // ------------------------------------------------------------------

    public sealed class DisposableFlag : IDisposable
    {
        public int DisposeCount { get; private set; }
        public void Dispose() => DisposeCount++;
    }

    [ServiceProvider]
    public partial class DisposalOrderingProvider;

    [Test]
    public async Task AddSingleton_Instance_IsDisposedWithContainer()
    {
        var flag = new DisposableFlag();

        await using var parent = await DisposalOrderingProvider.BuildAsync();

        var factory = new InjectNetServiceProviderFactory(parent);
        var services = new ServiceCollection();
        services.AddSingleton(flag);

        var builder = factory.CreateBuilder(services);
        var child = builder.Build();

        // Materialize so the descriptor factory runs and the instance is tracked.
        var resolved = ((System.IServiceProvider)child).GetService(typeof(DisposableFlag));
        await Assert.That(resolved).IsSameReferenceAs(flag);
        await Assert.That(flag.DisposeCount).IsEqualTo(0);

        await child.DisposeAsync();

        // MEDI semantics: the container owns AddSingleton(instance) and disposes it.
        await Assert.That(flag.DisposeCount).IsEqualTo(1);
    }

    [Test]
    public async Task RootProvider_Disposal_CascadesToTrackedChildren()
    {
        var flag = new DisposableFlag();

        var parent = await DisposalOrderingProvider.BuildAsync();

        var factory = new InjectNetServiceProviderFactory(parent);
        var services = new ServiceCollection();
        services.AddSingleton(flag);

        var child = factory.CreateServiceProvider(factory.CreateBuilder(services));

        // Trigger the factory so the instance is tracked by the child's singleton scope.
        _ = child.GetService(typeof(DisposableFlag));

        // Disposing the root should cascade to dispose the child we created through it.
        await parent.DisposeAsync();

        await Assert.That(flag.DisposeCount).IsEqualTo(1);
    }

    // ------------------------------------------------------------------
    // Builder guards
    // ------------------------------------------------------------------

    [Test]
    public async Task ContainerBuilder_Build_ThrowsWhenCalledTwice()
    {
        await using var parent = await DisposalOrderingProvider.BuildAsync();
        var factory = new InjectNetServiceProviderFactory(parent);
        var services = new ServiceCollection();

        var builder = factory.CreateBuilder(services);
        _ = builder.Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
        {
            builder.Build();
            return Task.CompletedTask;
        });
    }
}
