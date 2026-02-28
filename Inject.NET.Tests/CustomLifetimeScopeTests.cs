using System.Collections.Concurrent;
using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.Interfaces;
using Inject.NET.Services;

namespace Inject.NET.Tests;

public partial class CustomLifetimeScopeTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // PerThread Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task PerThread_ReturnsSameInstance_OnSameThread()
    {
        await using var serviceProvider = await PerThreadServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var instance1 = scope.GetRequiredService<IPerThreadService>();
        var instance2 = scope.GetRequiredService<IPerThreadService>();

        await Assert.That(instance1).IsSameReferenceAs(instance2);
    }

    [Test]
    public async Task PerThread_ReturnsDifferentInstances_OnDifferentThreads()
    {
        await using var serviceProvider = await PerThreadServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var instanceIds = new ConcurrentBag<string>();
        var tasks = new Task[10];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Factory.StartNew(() =>
            {
                var service = scope.GetRequiredService<IPerThreadService>();
                instanceIds.Add(service.Id);
            }, TaskCreationOptions.LongRunning);
        }

        await Task.WhenAll(tasks);

        var distinctIds = instanceIds.Distinct().ToArray();
        await Assert.That(distinctIds.Length).IsEqualTo(10);
    }

    [Test]
    public async Task PerThread_ReturnsSameInstance_WhenCalledMultipleTimesOnSameThread()
    {
        await using var serviceProvider = await PerThreadServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var ids = new ConcurrentBag<(int ThreadId, string ServiceId)>();
        var tasks = new Task[5];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Factory.StartNew(() =>
            {
                var threadId = Environment.CurrentManagedThreadId;

                // Resolve multiple times on the same thread
                var service1 = scope.GetRequiredService<IPerThreadService>();
                var service2 = scope.GetRequiredService<IPerThreadService>();
                var service3 = scope.GetRequiredService<IPerThreadService>();

                ids.Add((threadId, service1.Id));
                ids.Add((threadId, service2.Id));
                ids.Add((threadId, service3.Id));
            }, TaskCreationOptions.LongRunning);
        }

        await Task.WhenAll(tasks);

        // Group by thread ID - all services on same thread should have same ID
        var grouped = ids.GroupBy(x => x.ThreadId);
        foreach (var group in grouped)
        {
            var distinctServiceIds = group.Select(x => x.ServiceId).Distinct().ToArray();
            await Assert.That(distinctServiceIds.Length).IsEqualTo(1);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PerGraph Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task PerGraph_SharesInstance_WithinSingleResolveScope()
    {
        await using var serviceProvider = await PerGraphServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var graphScope = new PerGraphLifetimeScope();
        try
        {
            graphScope.BeginScope();

            var instance1 = graphScope.GetOrCreate(typeof(PerGraphService), null, () => new PerGraphService());
            var instance2 = graphScope.GetOrCreate(typeof(PerGraphService), null, () => new PerGraphService());

            await Assert.That(instance1).IsSameReferenceAs(instance2);

            graphScope.EndScope();
        }
        finally
        {
            graphScope.Dispose();
        }
    }

    [Test]
    public async Task PerGraph_CreatesNewInstance_ForSeparateResolveScopes()
    {
        var graphScope = new PerGraphLifetimeScope();
        try
        {
            // First resolution graph
            graphScope.BeginScope();
            var instance1 = graphScope.GetOrCreate(typeof(PerGraphService), null, () => new PerGraphService());
            graphScope.EndScope();

            // Second resolution graph
            graphScope.BeginScope();
            var instance2 = graphScope.GetOrCreate(typeof(PerGraphService), null, () => new PerGraphService());
            graphScope.EndScope();

            await Assert.That(instance1).IsNotSameReferenceAs(instance2);
        }
        finally
        {
            graphScope.Dispose();
        }
    }

    [Test]
    public async Task PerGraph_NestedBeginScope_ReusesExistingGraph()
    {
        var graphScope = new PerGraphLifetimeScope();
        try
        {
            graphScope.BeginScope();
            var instance1 = graphScope.GetOrCreate(typeof(PerGraphService), null, () => new PerGraphService());

            // Nested BeginScope should not create a new graph
            graphScope.BeginScope();
            var instance2 = graphScope.GetOrCreate(typeof(PerGraphService), null, () => new PerGraphService());

            await Assert.That(instance1).IsSameReferenceAs(instance2);

            graphScope.EndScope();
        }
        finally
        {
            graphScope.Dispose();
        }
    }

    [Test]
    public async Task PerGraph_WithoutBeginScope_CreatesNewInstanceEachTime()
    {
        var graphScope = new PerGraphLifetimeScope();
        try
        {
            // Without BeginScope, each call should create a new instance
            var instance1 = graphScope.GetOrCreate(typeof(PerGraphService), null, () => new PerGraphService());
            var instance2 = graphScope.GetOrCreate(typeof(PerGraphService), null, () => new PerGraphService());

            await Assert.That(instance1).IsNotSameReferenceAs(instance2);
        }
        finally
        {
            graphScope.Dispose();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Custom Lifetime Scope Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task CustomLifetimeScope_UserProvided_ControlsInstanceCaching()
    {
        await using var serviceProvider = await CustomLifetimeScopeServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var instance1 = scope.GetRequiredService<ICustomScopedService>();
        var instance2 = scope.GetRequiredService<ICustomScopedService>();

        // The custom scope always caches by type, so both should be the same
        await Assert.That(instance1).IsSameReferenceAs(instance2);
    }

    [Test]
    public async Task CustomLifetimeScope_EvictionPolicy_ReturnsNewInstanceAfterEviction()
    {
        var customScope = new EvictingLifetimeScope();

        var instance1 = customScope.GetOrCreate(typeof(PerGraphService), null, () => new PerGraphService());
        var instance2 = customScope.GetOrCreate(typeof(PerGraphService), null, () => new PerGraphService());

        // Same instance before eviction
        await Assert.That(instance1).IsSameReferenceAs(instance2);

        // Evict and get new instance
        customScope.Evict(typeof(PerGraphService));
        var instance3 = customScope.GetOrCreate(typeof(PerGraphService), null, () => new PerGraphService());

        await Assert.That(instance1).IsNotSameReferenceAs(instance3);

        customScope.Dispose();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Disposal Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task PerThread_Disposal_DisposesAllCachedInstances()
    {
        var perThreadScope = new PerThreadLifetimeScope();
        var disposable = new DisposableService();

        perThreadScope.GetOrCreate(typeof(DisposableService), null, () => disposable);

        await Assert.That(disposable.IsDisposed).IsFalse();

        perThreadScope.Dispose();

        await Assert.That(disposable.IsDisposed).IsTrue();
    }

    [Test]
    public async Task PerGraph_Disposal_DisposesAllCachedInstances()
    {
        var graphScope = new PerGraphLifetimeScope();
        var disposable = new DisposableService();

        graphScope.BeginScope();
        graphScope.GetOrCreate(typeof(DisposableService), null, () => disposable);

        await Assert.That(disposable.IsDisposed).IsFalse();

        graphScope.Dispose();

        await Assert.That(disposable.IsDisposed).IsTrue();
    }

    [Test]
    public async Task PerThread_ThrowsAfterDisposal()
    {
        var perThreadScope = new PerThreadLifetimeScope();
        perThreadScope.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            Task.FromResult(perThreadScope.GetOrCreate(typeof(PerGraphService), null, () => new PerGraphService()))
        );
    }

    [Test]
    public async Task PerGraph_ThrowsAfterDisposal()
    {
        var graphScope = new PerGraphLifetimeScope();
        graphScope.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
        {
            graphScope.BeginScope();
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task PerGraph_GetOrCreate_ThrowsAfterDisposal()
    {
        var graphScope = new PerGraphLifetimeScope();
        graphScope.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            Task.FromResult(graphScope.GetOrCreate(typeof(PerGraphService), null, () => new PerGraphService()))
        );
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Integration with Service Provider via Extension Methods
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task PerThread_IntegrationWithServiceProvider_ViaExtensionMethod()
    {
        await using var serviceProvider = await PerThreadServiceProvider.BuildAsync();

        var ids = new ConcurrentBag<(int ThreadId, string ServiceId)>();
        var tasks = new Task[5];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Factory.StartNew(() =>
            {
                using var scope = serviceProvider.CreateScope();
                var service = scope.GetRequiredService<IPerThreadService>();
                ids.Add((Environment.CurrentManagedThreadId, service.Id));
            }, TaskCreationOptions.LongRunning);
        }

        await Task.WhenAll(tasks);

        // Different threads should get different instances
        var distinctIds = ids.Select(x => x.ServiceId).Distinct().ToArray();
        await Assert.That(distinctIds.Length).IsEqualTo(5);
    }

    [Test]
    public async Task CustomLifetimeScope_WithFactory_RegistersAndResolvesCorrectly()
    {
        await using var serviceProvider = await FactoryCustomLifetimeServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service1 = scope.GetRequiredService<IConfigurableService>();
        var service2 = scope.GetRequiredService<IConfigurableService>();

        await Assert.That(service1).IsNotNull();
        await Assert.That(service1.Value).IsEqualTo("CustomValue");
        // The custom always-new scope should return new instances
        await Assert.That(service1).IsNotSameReferenceAs(service2);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Service Provider Definitions
    // ═══════════════════════════════════════════════════════════════════════

    [ServiceProvider]
    public partial class PerThreadServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddPerThread<IPerThreadService, PerThreadService>();
            }
        }
    }

    [ServiceProvider]
    public partial class PerGraphServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddPerGraph<IPerGraphService, PerGraphService>();
            }
        }
    }

    [ServiceProvider]
    public partial class CustomLifetimeScopeServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddWithLifetime<ICustomScopedService, CustomScopedService>(
                    new AlwaysCacheLifetimeScope());
            }
        }
    }

    [ServiceProvider]
    public partial class FactoryCustomLifetimeServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddWithLifetime<IConfigurableService>(
                    new AlwaysNewLifetimeScope(),
                    scope => new ConfigurableService("CustomValue"));
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Test Services
    // ═══════════════════════════════════════════════════════════════════════

    public interface IPerThreadService
    {
        string Id { get; }
    }

    public class PerThreadService : IPerThreadService
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
    }

    public interface IPerGraphService
    {
        string Id { get; }
    }

    public class PerGraphService : IPerGraphService
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
    }

    public interface ICustomScopedService
    {
        string Id { get; }
    }

    public class CustomScopedService : ICustomScopedService
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
    }

    public interface IConfigurableService
    {
        string Value { get; }
    }

    public class ConfigurableService(string value) : IConfigurableService
    {
        public string Value => value;
    }

    public class DisposableService : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Custom Lifetime Scope Implementations for Testing
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A simple custom lifetime scope that always caches instances (similar to singleton behavior).
    /// Used to verify that user-provided ILifetimeScope implementations work correctly.
    /// </summary>
    public class AlwaysCacheLifetimeScope : ILifetimeScope
    {
        private readonly Dictionary<(Type, string?), object> _cache = new();

        public object GetOrCreate(Type serviceType, string? key, Func<object> factory)
        {
            var cacheKey = (serviceType, key);
            if (_cache.TryGetValue(cacheKey, out var existing))
            {
                return existing;
            }

            var instance = factory();
            _cache[cacheKey] = instance;
            return instance;
        }

        public void Dispose()
        {
            foreach (var instance in _cache.Values)
            {
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _cache.Clear();
        }
    }

    /// <summary>
    /// A custom lifetime scope that never caches (every call creates a new instance).
    /// Used to verify factory-based registration with custom lifetime scopes.
    /// </summary>
    public class AlwaysNewLifetimeScope : ILifetimeScope
    {
        public object GetOrCreate(Type serviceType, string? key, Func<object> factory)
        {
            return factory();
        }

        public void Dispose()
        {
            // Nothing to dispose since nothing is cached
        }
    }

    /// <summary>
    /// A custom lifetime scope that supports manual eviction of cached instances.
    /// Demonstrates the extensibility of the ILifetimeScope interface.
    /// </summary>
    public class EvictingLifetimeScope : ILifetimeScope
    {
        private readonly Dictionary<(Type, string?), object> _cache = new();

        public object GetOrCreate(Type serviceType, string? key, Func<object> factory)
        {
            var cacheKey = (serviceType, key);
            if (_cache.TryGetValue(cacheKey, out var existing))
            {
                return existing;
            }

            var instance = factory();
            _cache[cacheKey] = instance;
            return instance;
        }

        public void Evict(Type serviceType, string? key = null)
        {
            var cacheKey = (serviceType, key);
            if (_cache.Remove(cacheKey, out var instance) && instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public void Dispose()
        {
            foreach (var instance in _cache.Values)
            {
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _cache.Clear();
        }
    }
}
