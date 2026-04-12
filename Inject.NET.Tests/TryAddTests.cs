using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

/// <summary>
/// Tests for TryAdd* conditional registration methods.
/// TryAdd only registers a service if no prior registration for that service type exists.
/// This is the standard pattern for library authors to provide default implementations.
/// </summary>
public partial class TryAddTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Test: TryAdd registers when no prior registration exists
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task TryAddSingleton_RegistersWhenNoPriorRegistration()
    {
        await using var serviceProvider = await TryAddSingletonNewServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IMyService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<DefaultService>();
    }

    [Test]
    public async Task TryAddScoped_RegistersWhenNoPriorRegistration()
    {
        await using var serviceProvider = await TryAddScopedNewServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IMyScopedService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<DefaultScopedService>();
    }

    [Test]
    public async Task TryAddTransient_RegistersWhenNoPriorRegistration()
    {
        await using var serviceProvider = await TryAddTransientNewServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IMyTransientService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<DefaultTransientService>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Test: TryAdd does NOT register when a prior registration exists
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task TryAddSingleton_SkipsWhenPriorRegistrationExists()
    {
        await using var serviceProvider = await TryAddSingletonExistingServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IMyService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<OverrideService>();
    }

    [Test]
    public async Task TryAddScoped_SkipsWhenPriorRegistrationExists()
    {
        await using var serviceProvider = await TryAddScopedExistingServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IMyScopedService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<OverrideScopedService>();
    }

    [Test]
    public async Task TryAddTransient_SkipsWhenPriorRegistrationExists()
    {
        await using var serviceProvider = await TryAddTransientExistingServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IMyTransientService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<OverrideTransientService>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Test: Multiple TryAdd calls for the same type (first wins)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task TryAddSingleton_MultipleCalls_FirstWins()
    {
        await using var serviceProvider = await TryAddMultipleServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IMyService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<DefaultService>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Test: Mixing Add and TryAdd (Add takes priority when called first)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task MixAddAndTryAdd_AddFirst_AddWins()
    {
        await using var serviceProvider = await MixAddTryAddServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IMyService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<OverrideService>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Test: TryAdd with factory delegate
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task TryAddSingleton_Factory_RegistersWhenNoPriorRegistration()
    {
        await using var serviceProvider = await TryAddFactoryNewServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IConfigService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service.Value).IsEqualTo("Default");
    }

    [Test]
    public async Task TryAddSingleton_Factory_SkipsWhenPriorRegistrationExists()
    {
        await using var serviceProvider = await TryAddFactoryExistingServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IConfigService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service.Value).IsEqualTo("Override");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Test: TryAdd with self-registration (single type parameter)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task TryAddSingleton_SelfRegistration_RegistersWhenNoPriorRegistration()
    {
        await using var serviceProvider = await TryAddSelfRegistrationServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ConcreteService>();

        await Assert.That(service).IsNotNull();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Test: TryAdd with attribute-registered service (attribute registered first)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task TryAdd_SkipsWhenAttributeRegistrationExists()
    {
        await using var serviceProvider = await TryAddWithAttributeServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IAttributeService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<AttributeServiceImpl>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Test: Singleton behavior is preserved with TryAdd
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task TryAddSingleton_PreservesSingletonBehavior()
    {
        await using var serviceProvider = await TryAddSingletonNewServiceProvider.BuildAsync();

        await using var scope1 = serviceProvider.CreateScope();
        var service1 = scope1.GetRequiredService<IMyService>();

        await using var scope2 = serviceProvider.CreateScope();
        var service2 = scope2.GetRequiredService<IMyService>();

        await Assert.That(service1).IsSameReferenceAs(service2);
    }

    [Test]
    public async Task TryAddScoped_PreservesScopedBehavior()
    {
        await using var serviceProvider = await TryAddScopedNewServiceProvider.BuildAsync();

        await using var scope1 = serviceProvider.CreateScope();
        var service1a = scope1.GetRequiredService<IMyScopedService>();
        var service1b = scope1.GetRequiredService<IMyScopedService>();

        await using var scope2 = serviceProvider.CreateScope();
        var service2 = scope2.GetRequiredService<IMyScopedService>();

        // Same instance within the same scope
        await Assert.That(service1a).IsSameReferenceAs(service1b);
        // Different instance across scopes
        await Assert.That(service1a).IsNotSameReferenceAs(service2);
    }

    [Test]
    public async Task TryAddTransient_PreservesTransientBehavior()
    {
        await using var serviceProvider = await TryAddTransientNewServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service1 = scope.GetRequiredService<IMyTransientService>();
        var service2 = scope.GetRequiredService<IMyTransientService>();

        // Different instances every time
        await Assert.That(service1).IsNotSameReferenceAs(service2);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Service Provider Definitions
    // ═══════════════════════════════════════════════════════════════════════

    // TryAdd registers when no prior registration
    [ServiceProvider]
    public partial class TryAddSingletonNewServiceProvider
    {
        static partial void ConfigureServices(global::Inject.NET.Interfaces.IServiceRegistrar registrar)
        {
            registrar.TryAddSingleton<IMyService, DefaultService>();
        }
    }

    [ServiceProvider]
    public partial class TryAddScopedNewServiceProvider
    {
        static partial void ConfigureServices(global::Inject.NET.Interfaces.IServiceRegistrar registrar)
        {
            registrar.TryAddScoped<IMyScopedService, DefaultScopedService>();
        }
    }

    [ServiceProvider]
    public partial class TryAddTransientNewServiceProvider
    {
        static partial void ConfigureServices(global::Inject.NET.Interfaces.IServiceRegistrar registrar)
        {
            registrar.TryAddTransient<IMyTransientService, DefaultTransientService>();
        }
    }

    // TryAdd skips when prior registration exists (Add first, TryAdd second)
    [ServiceProvider]
    public partial class TryAddSingletonExistingServiceProvider
    {
        static partial void ConfigureServices(global::Inject.NET.Interfaces.IServiceRegistrar registrar)
        {
            registrar.AddSingleton<IMyService, OverrideService>();
            registrar.TryAddSingleton<IMyService, DefaultService>();
        }
    }

    [ServiceProvider]
    public partial class TryAddScopedExistingServiceProvider
    {
        static partial void ConfigureServices(global::Inject.NET.Interfaces.IServiceRegistrar registrar)
        {
            registrar.AddScoped<IMyScopedService, OverrideScopedService>();
            registrar.TryAddScoped<IMyScopedService, DefaultScopedService>();
        }
    }

    [ServiceProvider]
    public partial class TryAddTransientExistingServiceProvider
    {
        static partial void ConfigureServices(global::Inject.NET.Interfaces.IServiceRegistrar registrar)
        {
            registrar.AddTransient<IMyTransientService, OverrideTransientService>();
            registrar.TryAddTransient<IMyTransientService, DefaultTransientService>();
        }
    }

    // Multiple TryAdd: first wins
    [ServiceProvider]
    public partial class TryAddMultipleServiceProvider
    {
        static partial void ConfigureServices(global::Inject.NET.Interfaces.IServiceRegistrar registrar)
        {
            registrar.TryAddSingleton<IMyService, DefaultService>();
            registrar.TryAddSingleton<IMyService, OverrideService>(); // should be skipped
        }
    }

    // Mix Add and TryAdd: Add registered first, TryAdd skips
    [ServiceProvider]
    public partial class MixAddTryAddServiceProvider
    {
        static partial void ConfigureServices(global::Inject.NET.Interfaces.IServiceRegistrar registrar)
        {
            registrar.AddSingleton<IMyService, OverrideService>();
            registrar.TryAddSingleton<IMyService, DefaultService>(); // should be skipped
        }
    }

    // TryAdd with factory delegate: no prior registration
    [ServiceProvider]
    public partial class TryAddFactoryNewServiceProvider
    {
        static partial void ConfigureServices(global::Inject.NET.Interfaces.IServiceRegistrar registrar)
        {
            registrar.TryAddSingleton<IConfigService>(_ => new ConfigServiceImpl("Default"));
        }
    }

    // TryAdd with factory delegate: prior registration exists
    [ServiceProvider]
    public partial class TryAddFactoryExistingServiceProvider
    {
        static partial void ConfigureServices(global::Inject.NET.Interfaces.IServiceRegistrar registrar)
        {
            registrar.AddSingleton<IConfigService>(_ => new ConfigServiceImpl("Override"));
            registrar.TryAddSingleton<IConfigService>(_ => new ConfigServiceImpl("Default")); // skipped
        }
    }

    // TryAdd self-registration (single type parameter)
    [ServiceProvider]
    public partial class TryAddSelfRegistrationServiceProvider
    {
        static partial void ConfigureServices(global::Inject.NET.Interfaces.IServiceRegistrar registrar)
        {
            registrar.TryAddSingleton<ConcreteService>();
        }
    }

    // TryAdd with attribute-registered service
    [ServiceProvider]
    [Singleton<IAttributeService, AttributeServiceImpl>]
    public partial class TryAddWithAttributeServiceProvider
    {
        static partial void ConfigureServices(global::Inject.NET.Interfaces.IServiceRegistrar registrar)
        {
            registrar.TryAddSingleton<IAttributeService, TryAddAttributeServiceImpl>(); // skipped because attribute registered first
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Test Services
    // ═══════════════════════════════════════════════════════════════════════

    public interface IMyService { }
    public class DefaultService : IMyService { }
    public class OverrideService : IMyService { }

    public interface IMyScopedService { }
    public class DefaultScopedService : IMyScopedService { }
    public class OverrideScopedService : IMyScopedService { }

    public interface IMyTransientService { }
    public class DefaultTransientService : IMyTransientService { }
    public class OverrideTransientService : IMyTransientService { }

    public interface IConfigService
    {
        string Value { get; }
    }

    public class ConfigServiceImpl(string value) : IConfigService
    {
        public string Value => value;
    }

    public class ConcreteService { }

    public interface IAttributeService { }
    public class AttributeServiceImpl : IAttributeService { }
    public class TryAddAttributeServiceImpl : IAttributeService { }
}
