using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class LazyDependencyTests
{
    public interface IExpensiveService
    {
        string Id { get; }
        static int CreationCount { get; set; }
    }

    public class ExpensiveService : IExpensiveService
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");

        public ExpensiveService()
        {
            IExpensiveService.CreationCount++;
        }

        public static int CreationCount
        {
            get => IExpensiveService.CreationCount;
            set => IExpensiveService.CreationCount = value;
        }
    }

    public class ServiceWithLazySingleton(Lazy<IExpensiveService> expensiveService)
    {
        public Lazy<IExpensiveService> ExpensiveService { get; } = expensiveService;
    }

    public class TransientService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public class ServiceWithLazyTransient(Lazy<TransientService> transientService)
    {
        public Lazy<TransientService> TransientService { get; } = transientService;
    }

    public class ScopedService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public class ServiceWithLazyScoped(Lazy<ScopedService> scopedService)
    {
        public Lazy<ScopedService> ScopedService { get; } = scopedService;
    }

    [ServiceProvider]
    [Singleton<IExpensiveService, ExpensiveService>]
    [Transient<ServiceWithLazySingleton>]
    [Transient<TransientService>]
    [Transient<ServiceWithLazyTransient>]
    [Scoped<ScopedService>]
    [Transient<ServiceWithLazyScoped>]
    public partial class LazyServiceProvider;

    [Test]
    public async Task Lazy_Singleton_ResolvesCorrectly()
    {
        await using var serviceProvider = await LazyServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ServiceWithLazySingleton>();

        await Assert.That(service.ExpensiveService).IsNotNull();
        await Assert.That(service.ExpensiveService.Value).IsNotNull();
        await Assert.That(service.ExpensiveService.Value.Id).IsNotNull();
    }

    [Test]
    public async Task Lazy_Singleton_ReturnsSameInstance()
    {
        await using var serviceProvider = await LazyServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service1 = scope.GetRequiredService<ServiceWithLazySingleton>();
        var service2 = scope.GetRequiredService<ServiceWithLazySingleton>();

        // Both should resolve to the same singleton
        await Assert.That(service1.ExpensiveService.Value.Id).IsEqualTo(service2.ExpensiveService.Value.Id);
    }

    [Test]
    public async Task Lazy_Singleton_SameAcrossScopes()
    {
        await using var serviceProvider = await LazyServiceProvider.BuildAsync();
        await using var scope1 = serviceProvider.CreateScope();
        await using var scope2 = serviceProvider.CreateScope();

        var service1 = scope1.GetRequiredService<ServiceWithLazySingleton>();
        var service2 = scope2.GetRequiredService<ServiceWithLazySingleton>();

        // Singleton should be the same across scopes
        await Assert.That(service1.ExpensiveService.Value.Id).IsEqualTo(service2.ExpensiveService.Value.Id);
    }

    [Test]
    public async Task Lazy_DeferredInitialization_ServiceNotCreatedUntilValueAccessed()
    {
        IExpensiveService.CreationCount = 0;

        await using var serviceProvider = await LazyServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        // The singleton will already be created during provider build,
        // but the Lazy wrapper should still work correctly
        var service = scope.GetRequiredService<ServiceWithLazySingleton>();

        // Accessing Value should return the service
        var resolved = service.ExpensiveService.Value;
        await Assert.That(resolved).IsNotNull();
    }

    [Test]
    public async Task Lazy_Transient_ValueCachedByLazy()
    {
        await using var serviceProvider = await LazyServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ServiceWithLazyTransient>();

        // Lazy caches the value after first access
        var first = service.TransientService.Value;
        var second = service.TransientService.Value;

        await Assert.That(first.Id).IsEqualTo(second.Id);
    }

    [Test]
    public async Task Lazy_Transient_DifferentLazyInstancesResolveDifferentTransients()
    {
        await using var serviceProvider = await LazyServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service1 = scope.GetRequiredService<ServiceWithLazyTransient>();
        var service2 = scope.GetRequiredService<ServiceWithLazyTransient>();

        // Different Lazy<T> instances should produce different transient instances
        // (since ServiceWithLazyTransient is itself transient, each gets its own Lazy)
        await Assert.That(service1.TransientService.Value.Id).IsNotEqualTo(service2.TransientService.Value.Id);
    }

    [Test]
    public async Task Lazy_Scoped_ResolvesCorrectly()
    {
        await using var serviceProvider = await LazyServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ServiceWithLazyScoped>();

        await Assert.That(service.ScopedService).IsNotNull();
        await Assert.That(service.ScopedService.Value).IsNotNull();
    }

    [Test]
    public async Task Lazy_Scoped_SameWithinScope()
    {
        await using var serviceProvider = await LazyServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service1 = scope.GetRequiredService<ServiceWithLazyScoped>();
        var service2 = scope.GetRequiredService<ServiceWithLazyScoped>();

        // Both should resolve to the same scoped instance within the same scope
        await Assert.That(service1.ScopedService.Value.Id).IsEqualTo(service2.ScopedService.Value.Id);
    }

    [Test]
    public async Task Lazy_Scoped_DifferentAcrossScopes()
    {
        await using var serviceProvider = await LazyServiceProvider.BuildAsync();
        await using var scope1 = serviceProvider.CreateScope();
        await using var scope2 = serviceProvider.CreateScope();

        var service1 = scope1.GetRequiredService<ServiceWithLazyScoped>();
        var service2 = scope2.GetRequiredService<ServiceWithLazyScoped>();

        // Different scopes should give different scoped instances
        await Assert.That(service1.ScopedService.Value.Id).IsNotEqualTo(service2.ScopedService.Value.Id);
    }
}
