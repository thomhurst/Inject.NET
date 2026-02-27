using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class PropertyInjectionTests
{
    [Test]
    public async Task InjectProperty_IsSetAfterConstruction()
    {
        await using var serviceProvider = await PropertyInjectionServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IServiceWithInjectProperty>();

        await Assert.That(service).IsNotNull();
        await Assert.That(((ServiceWithInjectProperty)service).InjectedDependency).IsNotNull();
    }

    [Test]
    public async Task InjectProperty_NullableProperty_IsOptional_WhenNotRegistered()
    {
        await using var serviceProvider = await NullablePropertyInjectionServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ServiceWithNullableInjectProperty>();

        await Assert.That(service).IsNotNull();
        // The nullable property should be null since UnregisteredDependency is not registered
        await Assert.That(service.OptionalDependency).IsNull();
        // The required property should still be set
        await Assert.That(service.RequiredDependency).IsNotNull();
    }

    [Test]
    public async Task InjectProperty_MultipleProperties_AllAreSet()
    {
        await using var serviceProvider = await MultiplePropertyInjectionServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ServiceWithMultipleInjectProperties>();

        await Assert.That(service.FirstDependency).IsNotNull();
        await Assert.That(service.SecondDependency).IsNotNull();
    }

    [Test]
    public async Task InjectProperty_WorksWithTransientLifetime()
    {
        await using var serviceProvider = await TransientPropertyInjectionServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var service1 = scope.GetRequiredService<TransientServiceWithInjectProperty>();
        var service2 = scope.GetRequiredService<TransientServiceWithInjectProperty>();

        await Assert.That(service1.InjectedDependency).IsNotNull();
        await Assert.That(service2.InjectedDependency).IsNotNull();
        await Assert.That(service1.Id).IsNotEqualTo(service2.Id);
    }

    [Test]
    public async Task InjectProperty_WorksWithScopedLifetime()
    {
        await using var serviceProvider = await ScopedPropertyInjectionServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ScopedServiceWithInjectProperty>();

        await Assert.That(service.InjectedDependency).IsNotNull();
    }

    [Test]
    public async Task InjectProperty_CombinedWithConstructorInjection()
    {
        await using var serviceProvider = await CombinedInjectionServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ServiceWithConstructorAndPropertyInjection>();

        // Constructor-injected dependency
        await Assert.That(service.ConstructorDependency).IsNotNull();
        // Property-injected dependency
        await Assert.That(service.PropertyDependency).IsNotNull();
    }

    [Test]
    public async Task InjectProperty_CombinedWithMethodInjection()
    {
        await using var serviceProvider = await CombinedMethodAndPropertyInjectionServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ServiceWithMethodAndPropertyInjection>();

        // Method-injected
        await Assert.That(service.WasInitialized).IsTrue();
        await Assert.That(service.MethodDependency).IsNotNull();
        // Property-injected
        await Assert.That(service.PropertyDependency).IsNotNull();
    }

    // --- Service definitions ---

    public interface IServiceWithInjectProperty;

    public class PropertyDependency
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
    }

    public class AnotherPropertyDependency
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
    }

    public class UnregisteredDependency;

    public class ServiceWithInjectProperty : IServiceWithInjectProperty
    {
        [Inject]
        public PropertyDependency InjectedDependency { get; set; } = null!;
    }

    public class ServiceWithNullableInjectProperty
    {
        [Inject]
        public PropertyDependency RequiredDependency { get; set; } = null!;

        [Inject]
        public UnregisteredDependency? OptionalDependency { get; set; }
    }

    public class ServiceWithMultipleInjectProperties
    {
        [Inject]
        public PropertyDependency FirstDependency { get; set; } = null!;

        [Inject]
        public AnotherPropertyDependency SecondDependency { get; set; } = null!;
    }

    public class TransientServiceWithInjectProperty
    {
        public Guid Id { get; } = Guid.NewGuid();

        [Inject]
        public PropertyDependency InjectedDependency { get; set; } = null!;
    }

    public class ScopedServiceWithInjectProperty
    {
        [Inject]
        public PropertyDependency InjectedDependency { get; set; } = null!;
    }

    public class ServiceWithConstructorAndPropertyInjection
    {
        public PropertyDependency ConstructorDependency { get; }

        [Inject]
        public AnotherPropertyDependency PropertyDependency { get; set; } = null!;

        public ServiceWithConstructorAndPropertyInjection(PropertyDependency constructorDependency)
        {
            ConstructorDependency = constructorDependency;
        }
    }

    public class ServiceWithMethodAndPropertyInjection
    {
        public bool WasInitialized { get; private set; }
        public PropertyDependency? MethodDependency { get; private set; }

        [Inject]
        public AnotherPropertyDependency PropertyDependency { get; set; } = null!;

        [Inject]
        public void Initialize(PropertyDependency dependency)
        {
            WasInitialized = true;
            MethodDependency = dependency;
        }
    }

    // --- Service providers ---

    [ServiceProvider]
    [Scoped<IServiceWithInjectProperty, ServiceWithInjectProperty>]
    [Scoped<PropertyDependency>]
    public partial class PropertyInjectionServiceProvider;

    [ServiceProvider]
    [Scoped<ServiceWithNullableInjectProperty>]
    [Scoped<PropertyDependency>]
    // Note: UnregisteredDependency is NOT registered, so the nullable property should be null
    public partial class NullablePropertyInjectionServiceProvider;

    [ServiceProvider]
    [Scoped<ServiceWithMultipleInjectProperties>]
    [Scoped<PropertyDependency>]
    [Scoped<AnotherPropertyDependency>]
    public partial class MultiplePropertyInjectionServiceProvider;

    [ServiceProvider]
    [Transient<TransientServiceWithInjectProperty>]
    [Transient<PropertyDependency>]
    public partial class TransientPropertyInjectionServiceProvider;

    [ServiceProvider]
    [Scoped<ScopedServiceWithInjectProperty>]
    [Scoped<PropertyDependency>]
    public partial class ScopedPropertyInjectionServiceProvider;

    [ServiceProvider]
    [Scoped<ServiceWithConstructorAndPropertyInjection>]
    [Scoped<PropertyDependency>]
    [Scoped<AnotherPropertyDependency>]
    public partial class CombinedInjectionServiceProvider;

    [ServiceProvider]
    [Scoped<ServiceWithMethodAndPropertyInjection>]
    [Scoped<PropertyDependency>]
    [Scoped<AnotherPropertyDependency>]
    public partial class CombinedMethodAndPropertyInjectionServiceProvider;
}
