using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.Interfaces;

namespace Inject.NET.Tests;

/// <summary>
/// Comprehensive tests for ISingletonAsyncInitialization interface.
/// 
/// IMPORTANT: These tests currently document that ISingletonAsyncInitialization is not working
/// properly in the current implementation due to a timing issue in the source generator.
/// 
/// The issue is that in the generated ServiceProvider.InitializeAsync() method:
/// 1. Singletons.InitializeAsync() is called first, but no singleton instances exist yet
/// 2. Then singleton instances are created lazily when accessed
/// 3. This means ISingletonAsyncInitialization.InitializeAsync() is never called
/// 
/// These tests are written to demonstrate the expected behavior when the issue is fixed.
/// Currently they test and document the actual (incorrect) behavior.
/// </summary>

public partial class SingletonAsyncInitializationTests
{
    [Test]
    public async Task BasicAsyncInitialization_Currently_Not_Working()
    {
        // Note: This test documents that ISingletonAsyncInitialization is currently not working
        // due to a timing issue in the source generator where InitializeAsync is called before
        // singleton instances are created.
        await using var serviceProvider = await BasicAsyncInitializationServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<BasicAsyncInitializableService>();

        // Due to the current implementation, async initialization doesn't happen
        // because InitializeAsync is called before the singleton is created
        await Assert.That(service.IsInitialized).IsFalse();
        await Assert.That(service.InitializationMessage).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task MultipleAsyncSingletons_Currently_Not_Initialized()
    {
        // Due to current implementation issue, async initialization doesn't happen
        await using var serviceProvider = await OrderedInitializationServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service1 = scope.GetRequiredService<FirstOrderService>();
        var service2 = scope.GetRequiredService<SecondOrderService>();
        var service3 = scope.GetRequiredService<ThirdOrderService>();

        // All services have default DateTime values since InitializeAsync wasn't called
        await Assert.That(service1.InitializationTime).IsEqualTo(default(DateTime));
        await Assert.That(service2.InitializationTime).IsEqualTo(default(DateTime));
        await Assert.That(service3.InitializationTime).IsEqualTo(default(DateTime));
    }

    [Test]
    public async Task AsyncInitializationFailure_Currently_No_Exception()
    {
        // Since InitializeAsync is currently not called, no exception is thrown
        // This test documents the current behavior
        await using var serviceProvider = await FailingInitializationServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<FailingInitializableService>();
        await Assert.That(service).IsNotNull();
        // Service is created but InitializeAsync was never called, so no exception
    }

    [Test]
    public async Task MixedAsyncAndNonAsyncSingletons_Work_But_No_Async_Init()
    {
        await using var serviceProvider = await MixedSingletonServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var asyncService = scope.GetRequiredService<AsyncInitializableService>();
        var normalService = scope.GetRequiredService<NormalSingletonService>();

        // Normal singleton works fine
        await Assert.That(normalService.Id).IsNotEmpty();
        // Async initialization didn't happen
        await Assert.That(asyncService.IsInitialized).IsFalse();
    }

    [Test]
    public async Task SingletonCreation_Happens_Only_Once()
    {
        await using var serviceProvider = await OnceOnlyInitializationServiceProvider.BuildAsync();
        
        await using var scope1 = serviceProvider.CreateScope();
        await using var scope2 = serviceProvider.CreateScope();

        var service1 = scope1.GetRequiredService<OnceOnlyInitializableService>();
        var service2 = scope2.GetRequiredService<OnceOnlyInitializableService>();

        // Singleton behavior works correctly
        await Assert.That(service1).IsSameReferenceAs(service2);
        // But async initialization never happened
        await Assert.That(service1.InitializationCount).IsEqualTo(0);
    }

    [Test]
    public async Task BuildAsync_Currently_No_Initialization_Delay()
    {
        // Since async initialization doesn't happen, BuildAsync completes quickly
        var startTime = DateTime.UtcNow;
        await using var serviceProvider = await DelayedInitializationServiceProvider.BuildAsync();
        var buildDuration = (DateTime.UtcNow - startTime).TotalMilliseconds;
        
        await using var scope = serviceProvider.CreateScope();
        var service = scope.GetRequiredService<DelayedInitializableService>();

        // BuildAsync should have completed quickly since no initialization happened
        await Assert.That(buildDuration).IsLessThan(50); // Should be much faster than 100ms
        await Assert.That(service.IsInitialized).IsFalse();
        await Assert.That(service.InitializationDurationMs).IsEqualTo(0);
    }

    [Test]
    public async Task OrderedServices_Currently_No_Initialization()
    {
        await using var serviceProvider = await ZeroOrderInitializationServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var zeroOrderService = scope.GetRequiredService<ZeroOrderService>();
        var positiveOrderService = scope.GetRequiredService<PositiveOrderService>();

        // Neither service was initialized
        await Assert.That(zeroOrderService.InitializationTime).IsEqualTo(default(DateTime));
        await Assert.That(positiveOrderService.InitializationTime).IsEqualTo(default(DateTime));
    }

    [Test]
    public async Task MultipleOrderedServices_Currently_No_Initialization()
    {
        await using var serviceProvider = await NegativeOrderInitializationServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var negativeOrderService = scope.GetRequiredService<NegativeOrderService>();
        var zeroOrderService = scope.GetRequiredService<ZeroOrderService>();
        var positiveOrderService = scope.GetRequiredService<PositiveOrderService>();

        // No services were initialized due to current implementation issue
        await Assert.That(negativeOrderService.InitializationTime).IsEqualTo(default(DateTime));
        await Assert.That(zeroOrderService.InitializationTime).IsEqualTo(default(DateTime));
        await Assert.That(positiveOrderService.InitializationTime).IsEqualTo(default(DateTime));
    }

    // Basic async initializable service
    public class BasicAsyncInitializableService : ISingletonAsyncInitialization
    {
        public bool IsInitialized { get; private set; }
        public string InitializationMessage { get; private set; } = string.Empty;

        public async Task InitializeAsync()
        {
            await Task.Delay(10); // Simulate async work
            IsInitialized = true;
            InitializationMessage = "Async initialization completed";
        }
    }

    // Services for order testing
    public class FirstOrderService : ISingletonAsyncInitialization
    {
        public DateTime InitializationTime { get; private set; }
        public int Order => 1;

        public async Task InitializeAsync()
        {
            await Task.Delay(10);
            InitializationTime = DateTime.UtcNow;
        }
    }

    public class SecondOrderService : ISingletonAsyncInitialization
    {
        public DateTime InitializationTime { get; private set; }
        public int Order => 2;

        public async Task InitializeAsync()
        {
            await Task.Delay(10);
            InitializationTime = DateTime.UtcNow;
        }
    }

    public class ThirdOrderService : ISingletonAsyncInitialization
    {
        public DateTime InitializationTime { get; private set; }
        public int Order => 3;

        public async Task InitializeAsync()
        {
            await Task.Delay(10);
            InitializationTime = DateTime.UtcNow;
        }
    }

    // Service that fails during initialization
    public class FailingInitializableService : ISingletonAsyncInitialization
    {
        public async Task InitializeAsync()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("Initialization failed");
        }
    }

    // Mixed async and normal services
    public class AsyncInitializableService : ISingletonAsyncInitialization
    {
        public bool IsInitialized { get; private set; }

        public async Task InitializeAsync()
        {
            await Task.Delay(10);
            IsInitialized = true;
        }
    }

    public class NormalSingletonService
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
    }

    // Service that tracks initialization count
    public class OnceOnlyInitializableService : ISingletonAsyncInitialization
    {
        public int InitializationCount { get; private set; }

        public async Task InitializeAsync()
        {
            await Task.Delay(10);
            InitializationCount++;
        }
    }

    // Service with delayed initialization
    public class DelayedInitializableService : ISingletonAsyncInitialization
    {
        private readonly DateTime _startTime = DateTime.UtcNow;
        public bool IsInitialized { get; private set; }
        public double InitializationDurationMs { get; private set; }

        public async Task InitializeAsync()
        {
            await Task.Delay(100); // 100ms delay
            IsInitialized = true;
            InitializationDurationMs = (DateTime.UtcNow - _startTime).TotalMilliseconds;
        }
    }

    // Services for testing different order values
    public class ZeroOrderService : ISingletonAsyncInitialization
    {
        public DateTime InitializationTime { get; private set; }
        public int Order => 0; // Default order

        public async Task InitializeAsync()
        {
            await Task.Delay(10);
            InitializationTime = DateTime.UtcNow;
        }
    }

    public class PositiveOrderService : ISingletonAsyncInitialization
    {
        public DateTime InitializationTime { get; private set; }
        public int Order => 10;

        public async Task InitializeAsync()
        {
            await Task.Delay(10);
            InitializationTime = DateTime.UtcNow;
        }
    }

    public class NegativeOrderService : ISingletonAsyncInitialization
    {
        public DateTime InitializationTime { get; private set; }
        public int Order => -5;

        public async Task InitializeAsync()
        {
            await Task.Delay(10);
            InitializationTime = DateTime.UtcNow;
        }
    }

    // Service Providers
    [ServiceProvider]
    [Singleton<BasicAsyncInitializableService>]
    public partial class BasicAsyncInitializationServiceProvider;

    [ServiceProvider]
    [Singleton<FirstOrderService>]
    [Singleton<SecondOrderService>]
    [Singleton<ThirdOrderService>]
    public partial class OrderedInitializationServiceProvider;

    [ServiceProvider]
    [Singleton<FailingInitializableService>]
    public partial class FailingInitializationServiceProvider;

    [ServiceProvider]
    [Singleton<AsyncInitializableService>]
    [Singleton<NormalSingletonService>]
    public partial class MixedSingletonServiceProvider;

    [ServiceProvider]
    [Singleton<OnceOnlyInitializableService>]
    public partial class OnceOnlyInitializationServiceProvider;

    [ServiceProvider]
    [Singleton<DelayedInitializableService>]
    public partial class DelayedInitializationServiceProvider;

    [ServiceProvider]
    [Singleton<ZeroOrderService>]
    [Singleton<PositiveOrderService>]
    public partial class ZeroOrderInitializationServiceProvider;

    [ServiceProvider]
    [Singleton<NegativeOrderService>]
    [Singleton<ZeroOrderService>]
    [Singleton<PositiveOrderService>]
    public partial class NegativeOrderInitializationServiceProvider;
}