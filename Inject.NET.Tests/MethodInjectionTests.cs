using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class MethodInjectionTests
{
    [Test]
    public async Task InjectMethod_IsCalledAfterConstruction()
    {
        await using var serviceProvider = await MethodInjectionServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IServiceWithInjectMethod>();

        await Assert.That(service).IsNotNull();
        await Assert.That(((ServiceWithInjectMethod)service).WasInitialized).IsTrue();
    }

    [Test]
    public async Task InjectMethod_DependenciesAreResolved()
    {
        await using var serviceProvider = await MethodInjectionServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IServiceWithInjectMethod>();
        var impl = (ServiceWithInjectMethod)service;

        await Assert.That(impl.InjectedDependency).IsNotNull();
    }

    [Test]
    public async Task MultipleInjectMethods_AllAreCalled()
    {
        await using var serviceProvider = await MultipleInjectMethodServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ServiceWithMultipleInjectMethods>();

        await Assert.That(service.FirstMethodCalled).IsTrue();
        await Assert.That(service.SecondMethodCalled).IsTrue();
        await Assert.That(service.FirstDependency).IsNotNull();
        await Assert.That(service.SecondDependency).IsNotNull();
    }

    [Test]
    public async Task InjectMethod_WorksWithTransientLifetime()
    {
        await using var serviceProvider = await TransientMethodInjectionServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var service1 = scope.GetRequiredService<TransientServiceWithInjectMethod>();
        var service2 = scope.GetRequiredService<TransientServiceWithInjectMethod>();

        await Assert.That(service1.WasInitialized).IsTrue();
        await Assert.That(service2.WasInitialized).IsTrue();
        await Assert.That(service1.Id).IsNotEqualTo(service2.Id);
    }

    [Test]
    public async Task InjectMethod_WorksWithScopedLifetime()
    {
        await using var serviceProvider = await ScopedMethodInjectionServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ScopedServiceWithInjectMethod>();

        await Assert.That(service.WasInitialized).IsTrue();
        await Assert.That(service.InjectedDependency).IsNotNull();
    }

    [Test]
    public async Task InjectMethod_AsyncMethod_IsAwaited()
    {
        await using var serviceProvider = await AsyncMethodInjectionServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ServiceWithAsyncInjectMethod>();

        await Assert.That(service.WasInitialized).IsTrue();
        await Assert.That(service.InjectedDependency).IsNotNull();
    }

    // --- Service definitions ---

    public interface IServiceWithInjectMethod;

    public class SimpleInjectedDependency
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
    }

    public class AnotherDependency
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
    }

    public class ServiceWithInjectMethod : IServiceWithInjectMethod
    {
        public bool WasInitialized { get; private set; }
        public SimpleInjectedDependency? InjectedDependency { get; private set; }

        [Inject]
        public void Initialize(SimpleInjectedDependency dependency)
        {
            WasInitialized = true;
            InjectedDependency = dependency;
        }
    }

    public class ServiceWithMultipleInjectMethods
    {
        public bool FirstMethodCalled { get; private set; }
        public bool SecondMethodCalled { get; private set; }
        public SimpleInjectedDependency? FirstDependency { get; private set; }
        public AnotherDependency? SecondDependency { get; private set; }

        [Inject]
        public void InitializeFirst(SimpleInjectedDependency dependency)
        {
            FirstMethodCalled = true;
            FirstDependency = dependency;
        }

        [Inject]
        public void InitializeSecond(AnotherDependency dependency)
        {
            SecondMethodCalled = true;
            SecondDependency = dependency;
        }
    }

    public class TransientServiceWithInjectMethod
    {
        public Guid Id { get; } = Guid.NewGuid();
        public bool WasInitialized { get; private set; }

        [Inject]
        public void Initialize(SimpleInjectedDependency dependency)
        {
            WasInitialized = true;
        }
    }

    public class ScopedServiceWithInjectMethod
    {
        public bool WasInitialized { get; private set; }
        public SimpleInjectedDependency? InjectedDependency { get; private set; }

        [Inject]
        public void Initialize(SimpleInjectedDependency dependency)
        {
            WasInitialized = true;
            InjectedDependency = dependency;
        }
    }

    public class ServiceWithAsyncInjectMethod
    {
        public bool WasInitialized { get; private set; }
        public SimpleInjectedDependency? InjectedDependency { get; private set; }

        [Inject]
        public async Task InitializeAsync(SimpleInjectedDependency dependency)
        {
            await Task.CompletedTask;
            WasInitialized = true;
            InjectedDependency = dependency;
        }
    }

    // --- Service providers ---

    [ServiceProvider]
    [Scoped<IServiceWithInjectMethod, ServiceWithInjectMethod>]
    [Scoped<SimpleInjectedDependency>]
    public partial class MethodInjectionServiceProvider;

    [ServiceProvider]
    [Scoped<ServiceWithMultipleInjectMethods>]
    [Scoped<SimpleInjectedDependency>]
    [Scoped<AnotherDependency>]
    public partial class MultipleInjectMethodServiceProvider;

    [ServiceProvider]
    [Transient<TransientServiceWithInjectMethod>]
    [Transient<SimpleInjectedDependency>]
    public partial class TransientMethodInjectionServiceProvider;

    [ServiceProvider]
    [Scoped<ScopedServiceWithInjectMethod>]
    [Scoped<SimpleInjectedDependency>]
    public partial class ScopedMethodInjectionServiceProvider;

    [ServiceProvider]
    [Scoped<ServiceWithAsyncInjectMethod>]
    [Scoped<SimpleInjectedDependency>]
    public partial class AsyncMethodInjectionServiceProvider;
}
