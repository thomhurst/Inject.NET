using Inject.NET.Attributes;
using Inject.NET.Extensions;
using System.Collections.Concurrent;

namespace Inject.NET.Tests;

public partial class ConcurrencyTests
{
    [Test]
    public async Task ConcurrentServiceResolution_SingletonService_MultipleTasks()
    {
        await using var serviceProvider = await ConcurrencyServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var instanceIds = new ConcurrentBag<string>();
        var tasks = new Task[100];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                var service = scope.GetRequiredService<SingletonService>();
                instanceIds.Add(service.Id);
            });
        }

        await Task.WhenAll(tasks);

        // All singleton instances should have the same ID
        var distinctIds = instanceIds.Distinct().ToArray();
        await Assert.That(distinctIds.Length).IsEqualTo(1);
    }

    [Test]
    public async Task ConcurrentServiceResolution_TransientService_MultipleTasks()
    {
        await using var serviceProvider = await ConcurrencyServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var instanceIds = new ConcurrentBag<string>();
        var tasks = new Task[100];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                var service = scope.GetRequiredService<TransientService>();
                instanceIds.Add(service.Id);
            });
        }

        await Task.WhenAll(tasks);

        // All transient instances should have different IDs
        var distinctIds = instanceIds.Distinct().ToArray();
        await Assert.That(distinctIds.Length).IsEqualTo(100);
    }

    [Test]
    public async Task ConcurrentServiceResolution_ScopedService_SingleScope()
    {
        await using var serviceProvider = await ConcurrencyServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var instanceIds = new ConcurrentBag<string>();
        var tasks = new Task[100];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                var service = scope.GetRequiredService<ScopedService>();
                instanceIds.Add(service.Id);
            });
        }

        await Task.WhenAll(tasks);

        // All scoped instances within the same scope should have the same ID
        var distinctIds = instanceIds.Distinct().ToArray();
        await Assert.That(distinctIds.Length).IsEqualTo(1);
    }

    [Test]
    public async Task ConcurrentScopeCreationAndDisposal()
    {
        await using var serviceProvider = await ConcurrencyServiceProvider.BuildAsync();

        var scopeResults = new ConcurrentBag<(string ScopedId, string SingletonId)>();
        var tasks = new Task[50];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                await using var scope = serviceProvider.CreateScope();
                
                var scopedService = scope.GetRequiredService<ScopedService>();
                var singletonService = scope.GetRequiredService<SingletonService>();
                
                scopeResults.Add((scopedService.Id, singletonService.Id));
                
                // Simulate some work
                await Task.Delay(Random.Shared.Next(1, 10));
            });
        }

        await Task.WhenAll(tasks);

        var results = scopeResults.ToArray();
        
        // All singleton instances should have the same ID
        var singletonIds = results.Select(r => r.SingletonId).Distinct().ToArray();
        await Assert.That(singletonIds.Length).IsEqualTo(1);
        
        // All scoped instances should have different IDs (different scopes)
        var scopedIds = results.Select(r => r.ScopedId).Distinct().ToArray();
        await Assert.That(scopedIds.Length).IsEqualTo(50);
    }

    [Test]
    public async Task ThreadSafeSingletonAccess_VerifyTrulySingleInstance()
    {
        await using var serviceProvider = await ConcurrencyServiceProvider.BuildAsync();

        var creationTimestamps = new ConcurrentBag<DateTime>();
        var instanceIds = new ConcurrentBag<string>();
        var tasks = new Task[200];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                await using var scope = serviceProvider.CreateScope();
                var service = scope.GetRequiredService<TimestampedSingletonService>();
                
                creationTimestamps.Add(service.CreatedAt);
                instanceIds.Add(service.Id);
            });
        }

        await Task.WhenAll(tasks);

        // Verify all instances are the same
        var distinctIds = instanceIds.Distinct().ToArray();
        await Assert.That(distinctIds.Length).IsEqualTo(1);
        
        // Verify singleton was created only once
        var distinctTimestamps = creationTimestamps.Distinct().ToArray();
        await Assert.That(distinctTimestamps.Length).IsEqualTo(1);
    }

    [Test]
    public async Task ConcurrentServiceProviderDisposal()
    {
        var serviceProvider = await ConcurrencyServiceProvider.BuildAsync();
        var scopes = new List<Inject.NET.Interfaces.IServiceScope>();
        var tasks = new Task[20];

        // Create multiple scopes concurrently
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                var scope = serviceProvider.CreateScope();
                lock (scopes)
                {
                    scopes.Add(scope);
                }
                
                // Use services to ensure they're initialized
                scope.GetRequiredService<SingletonService>();
                scope.GetRequiredService<ScopedService>();
                scope.GetRequiredService<TransientService>();
            });
        }

        await Task.WhenAll(tasks);

        // Dispose all scopes concurrently
        var disposalTasks = scopes.Select(scope => Task.Run(async () =>
        {
            await scope.DisposeAsync();
        })).ToArray();

        await Task.WhenAll(disposalTasks);

        // Finally dispose the service provider
        await serviceProvider.DisposeAsync();

        // Test should complete without deadlocks or exceptions
        await Assert.That(scopes.Count).IsEqualTo(20);
    }

    [Test]
    public async Task RaceConditionsInServiceInitialization_WithLazyInitialization()
    {
        await using var serviceProvider = await ConcurrencyServiceProvider.BuildAsync();

        var initializationResults = new ConcurrentBag<string>();
        var tasks = new Task[100];

        for (int i = 0; i < tasks.Length; i++)
        {
            int taskId = i;
            tasks[i] = Task.Run(async () =>
            {
                await using var scope = serviceProvider.CreateScope();
                var service = scope.GetRequiredService<LazyInitializationService>();
                
                // Trigger initialization
                var result = await service.InitializeAsync(taskId);
                initializationResults.Add(result);
            });
        }

        await Task.WhenAll(tasks);

        // Verify that initialization happened only once despite concurrent access
        var results = initializationResults.ToArray();
        var distinctResults = results.Distinct().ToArray();
        
        // Should only have one unique initialization result
        await Assert.That(distinctResults.Length).IsEqualTo(1);
        
        // But we should have received it 100 times
        await Assert.That(results.Length).IsEqualTo(100);
    }

    [Test]
    public async Task StressTest_HighFrequencyServiceResolution()
    {
        await using var serviceProvider = await ConcurrencyServiceProvider.BuildAsync();

        var totalResolutions = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var duration = TimeSpan.FromSeconds(2);

        var tasks = Enumerable.Range(0, Environment.ProcessorCount).Select(_ => Task.Run(async () =>
        {
            var localResolutions = 0;
            while (stopwatch.Elapsed < duration)
            {
                await using var scope = serviceProvider.CreateScope();
                
                // Resolve multiple services rapidly
                scope.GetRequiredService<SingletonService>();
                scope.GetRequiredService<ScopedService>();
                scope.GetRequiredService<TransientService>();
                scope.GetRequiredService<ComplexDependencyService>();
                
                localResolutions += 4;
            }
            
            Interlocked.Add(ref totalResolutions, localResolutions);
        })).ToArray();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Verify high throughput without exceptions
        await Assert.That(totalResolutions).IsGreaterThan(1000);
        
        Console.WriteLine($"Completed {totalResolutions} service resolutions in {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
    }

    [Test]
    public async Task ConcurrentScopeAccess_WithComplexDependencyChain()
    {
        await using var serviceProvider = await ConcurrencyServiceProvider.BuildAsync();

        var results = new ConcurrentBag<(string ComplexId, string SingletonId, string ScopedId)>();
        var tasks = new Task[50];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                await using var scope = serviceProvider.CreateScope();
                
                var complexService = scope.GetRequiredService<ComplexDependencyService>();
                
                results.Add((
                    complexService.Id,
                    complexService.SingletonDependency.Id,
                    complexService.ScopedDependency.Id
                ));
                
                // Simulate work
                await Task.Delay(Random.Shared.Next(1, 5));
            });
        }

        await Task.WhenAll(tasks);

        var resultArray = results.ToArray();
        
        // All singleton dependencies should be the same
        var singletonIds = resultArray.Select(r => r.SingletonId).Distinct().ToArray();
        await Assert.That(singletonIds.Length).IsEqualTo(1);
        
        // All scoped dependencies should be different (different scopes)
        var scopedIds = resultArray.Select(r => r.ScopedId).Distinct().ToArray();
        await Assert.That(scopedIds.Length).IsEqualTo(50);
        
        // All complex service instances should be different (transient)
        var complexIds = resultArray.Select(r => r.ComplexId).Distinct().ToArray();
        await Assert.That(complexIds.Length).IsEqualTo(50);
    }

    [Test]
    public async Task ParallelFor_ConcurrentServiceResolution()
    {
        await using var serviceProvider = await ConcurrencyServiceProvider.BuildAsync();

        var instanceIds = new ConcurrentBag<string>();
        
        Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async i =>
        {
            await using var scope = serviceProvider.CreateScope();
            var service = scope.GetRequiredService<SingletonService>();
            instanceIds.Add(service.Id);
        });

        // All singleton instances should have the same ID
        var distinctIds = instanceIds.Distinct().ToArray();
        await Assert.That(distinctIds.Length).IsEqualTo(1);
        await Assert.That(instanceIds.Count).IsEqualTo(1000);
    }

    // Service classes for testing
    public class SingletonService
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
    }

    public class ScopedService
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
    }

    public class TransientService
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
    }

    public class TimestampedSingletonService
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    public class LazyInitializationService
    {
        private readonly Lazy<Task<string>> _initialization;
        public string Id { get; } = Guid.NewGuid().ToString("N");

        public LazyInitializationService()
        {
            _initialization = new Lazy<Task<string>>(async () =>
            {
                // Simulate expensive initialization
                await Task.Delay(50);
                return $"Initialized-{DateTime.UtcNow.Ticks}";
            });
        }

        public async Task<string> InitializeAsync(int taskId)
        {
            var result = await _initialization.Value;
            return $"{result}-Task{taskId}";
        }
    }

    public class ComplexDependencyService
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public SingletonService SingletonDependency { get; }
        public ScopedService ScopedDependency { get; }
        public TransientService TransientDependency { get; }

        public ComplexDependencyService(
            SingletonService singletonDependency,
            ScopedService scopedDependency,
            TransientService transientDependency)
        {
            SingletonDependency = singletonDependency;
            ScopedDependency = scopedDependency;
            TransientDependency = transientDependency;
        }
    }

    [ServiceProvider]
    [Singleton<SingletonService>]
    [Singleton<TimestampedSingletonService>]
    [Singleton<LazyInitializationService>]
    [Scoped<ScopedService>]
    [Transient<TransientService>]
    [Transient<ComplexDependencyService>]
    public partial class ConcurrencyServiceProvider;
}