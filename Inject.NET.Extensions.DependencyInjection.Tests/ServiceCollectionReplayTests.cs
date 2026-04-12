using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Inject.NET.Extensions.DependencyInjection.Tests;

public partial class ServiceCollectionReplayTests
{
    public interface IService
    {
        string Name { get; }
    }

    public class ServiceImpl : IService
    {
        public string Name => "TypeBased";
    }

    public class FactoryService : IService
    {
        public string Name => "FactoryBased";
    }

    public class InstanceService : IService
    {
        public string Name { get; }

        public InstanceService(string name)
        {
            Name = name;
        }
    }

    public class ScopedService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public class TransientService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    // --- Type-based registration provider ---

    [ServiceProvider]
    public partial class TypeBasedProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddServiceCollection(services =>
                {
                    services.AddSingleton<IService, ServiceImpl>();
                });
            }
        }
    }

    [Test]
    public async Task TypeBasedRegistration_ResolvesCorrectly()
    {
        await using var serviceProvider = await TypeBasedProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service.Name).IsEqualTo("TypeBased");
    }

    // --- Factory-based registration provider ---

    [ServiceProvider]
    public partial class FactoryBasedProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddServiceCollection(services =>
                {
                    services.AddSingleton<IService>(sp => new FactoryService());
                });
            }
        }
    }

    [Test]
    public async Task FactoryBasedRegistration_ResolvesCorrectly()
    {
        await using var serviceProvider = await FactoryBasedProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service.Name).IsEqualTo("FactoryBased");
    }

    // --- Instance-based registration provider ---

    [ServiceProvider]
    public partial class InstanceBasedProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                var instance = new InstanceService("PreCreated");
                this.AddServiceCollection(services =>
                {
                    services.AddSingleton<IService>(instance);
                });
            }
        }
    }

    [Test]
    public async Task InstanceBasedRegistration_ResolvesCorrectly()
    {
        await using var serviceProvider = await InstanceBasedProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service.Name).IsEqualTo("PreCreated");
    }

    // --- Scoped lifetime provider ---

    [ServiceProvider]
    public partial class ScopedLifetimeProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddServiceCollection(services =>
                {
                    services.AddScoped<ScopedService>();
                });
            }
        }
    }

    [Test]
    public async Task ScopedLifetime_CreatesNewInstancePerScope()
    {
        await using var serviceProvider = await ScopedLifetimeProvider.BuildAsync();

        await using var scope1 = serviceProvider.CreateScope();
        await using var scope2 = serviceProvider.CreateScope();

        var service1a = scope1.GetRequiredService<ScopedService>();
        var service1b = scope1.GetRequiredService<ScopedService>();
        var service2 = scope2.GetRequiredService<ScopedService>();

        // Same within scope
        await Assert.That(service1a.Id).IsEqualTo(service1b.Id);
        // Different across scopes
        await Assert.That(service1a.Id).IsNotEqualTo(service2.Id);
    }

    // --- Transient lifetime provider ---

    [ServiceProvider]
    public partial class TransientLifetimeProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddServiceCollection(services =>
                {
                    services.AddTransient<TransientService>();
                });
            }
        }
    }

    [Test]
    public async Task TransientLifetime_CreatesNewInstanceEveryTime()
    {
        await using var serviceProvider = await TransientLifetimeProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service1 = scope.GetRequiredService<TransientService>();
        var service2 = scope.GetRequiredService<TransientService>();

        await Assert.That(service1.Id).IsNotEqualTo(service2.Id);
    }
}
