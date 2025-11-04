using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

/// <summary>
/// Tests for extension method-based service registration.
/// Demonstrates the new flexible registration API that complements attribute-based registration.
/// </summary>
public partial class ExtensionMethodTests
{
    [Test]
    public async Task AddSingleton_RegistersAndResolvesSingletonService()
    {
        await using var serviceProvider = await ExtensionSingletonServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service1 = scope.GetRequiredService<ISingletonService>();
        var service2 = scope.GetRequiredService<ISingletonService>();

        await Assert.That(service1).IsNotNull();
        await Assert.That(service2).IsNotNull();
        await Assert.That(service1).IsSameReferenceAs(service2);
    }

    [Test]
    public async Task AddScoped_RegistersAndResolvesScopedService()
    {
        await using var serviceProvider = await ExtensionScopedServiceProvider.BuildAsync();

        await using var scope1 = serviceProvider.CreateScope();
        var service1a = scope1.GetRequiredService<IScopedService>();
        var service1b = scope1.GetRequiredService<IScopedService>();

        await using var scope2 = serviceProvider.CreateScope();
        var service2 = scope2.GetRequiredService<IScopedService>();

        await Assert.That(service1a).IsSameReferenceAs(service1b);
        await Assert.That(service1a).IsNotSameReferenceAs(service2);
    }

    [Test]
    public async Task AddTransient_RegistersAndResolvesTransientService()
    {
        await using var serviceProvider = await ExtensionTransientServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service1 = scope.GetRequiredService<ITransientService>();
        var service2 = scope.GetRequiredService<ITransientService>();

        await Assert.That(service1).IsNotNull();
        await Assert.That(service2).IsNotNull();
        await Assert.That(service1).IsNotSameReferenceAs(service2);
    }

    [Test]
    public async Task FluentChaining_WorksAcrossMultipleRegistrations()
    {
        await using var serviceProvider = await FluentChainServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var singleton = scope.GetRequiredService<ISingletonService>();
        var scoped = scope.GetRequiredService<IScopedService>();
        var transient = scope.GetRequiredService<ITransientService>();

        await Assert.That(singleton).IsNotNull();
        await Assert.That(scoped).IsNotNull();
        await Assert.That(transient).IsNotNull();
    }

    [Test]
    public async Task FactoryDelegate_Singleton_CreatesServiceWithCustomLogic()
    {
        await using var serviceProvider = await FactorySingletonServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IConfigService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service.Value).IsEqualTo("Custom Factory Value");
    }

    [Test]
    public async Task MixedRegistration_AttributesAndExtensions_BothWork()
    {
        await using var serviceProvider = await MixedRegistrationServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        // Attribute-registered service
        var attributeService = scope.GetRequiredService<AttributeRegisteredService>();

        // Extension-registered service
        var extensionService = scope.GetRequiredService<IExtensionRegisteredService>();

        await Assert.That(attributeService).IsNotNull();
        await Assert.That(extensionService).IsNotNull();
    }

    [Test]
    public async Task DependencyInjection_ExtensionRegisteredService_ResolvesDependencies()
    {
        await using var serviceProvider = await DependencyInjectionServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IServiceWithDependency>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service.Dependency).IsNotNull();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Service Provider Definitions
    // ═══════════════════════════════════════════════════════════════════════

    [ServiceProvider]
    public partial class ExtensionSingletonServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<ISingletonService, SingletonService>();
            }
        }
    }

    [ServiceProvider]
    public partial class ExtensionScopedServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddScoped<IScopedService, ScopedService>();
            }
        }
    }

    [ServiceProvider]
    public partial class ExtensionTransientServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddTransient<ITransientService, TransientService>();
            }
        }
    }

    [ServiceProvider]
    public partial class FluentChainServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<ISingletonService, SingletonService>()
                    .AddScoped<IScopedService, ScopedService>()
                    .AddTransient<ITransientService, TransientService>();
            }
        }
    }

    [ServiceProvider]
    public partial class FactorySingletonServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<IConfigService>(scope =>
                    new ConfigService("Custom Factory Value"));
            }
        }
    }

    [ServiceProvider]
    [Singleton<AttributeRegisteredService>]
    public partial class MixedRegistrationServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<IExtensionRegisteredService, ExtensionRegisteredService>();
            }
        }
    }

    [ServiceProvider]
    public partial class DependencyInjectionServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<IDependency, Dependency>()
                    .AddSingleton<IServiceWithDependency, ServiceWithDependency>();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Test Services
    // ═══════════════════════════════════════════════════════════════════════

    public interface ISingletonService { }
    public class SingletonService : ISingletonService { }

    public interface IScopedService { }
    public class ScopedService : IScopedService { }

    public interface ITransientService { }
    public class TransientService : ITransientService { }

    public interface IConfigService
    {
        string Value { get; }
    }

    public class ConfigService(string value) : IConfigService
    {
        public string Value => value;
    }

    public class AttributeRegisteredService { }

    public interface IExtensionRegisteredService { }
    public class ExtensionRegisteredService : IExtensionRegisteredService { }

    public interface IDependency { }
    public class Dependency : IDependency { }

    public interface IServiceWithDependency
    {
        IDependency Dependency { get; }
    }

    public class ServiceWithDependency(IDependency dependency) : IServiceWithDependency
    {
        public IDependency Dependency => dependency;
    }
}
