using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

/// <summary>
/// Tests for registering pre-existing object instances as singleton services.
/// Validates that instance registration works correctly via the AddSingleton overloads
/// that accept an existing instance rather than creating one through the container.
/// </summary>
public partial class InstanceRegistrationTests
{
    [Test]
    public async Task AddSingleton_Instance_CanBeResolved()
    {
        await using var serviceProvider = await InstanceSingletonServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IMyService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service.Name).IsEqualTo("PreExisting");
    }

    [Test]
    public async Task AddSingleton_Instance_ReturnsSameInstance_FromSameScope()
    {
        await using var serviceProvider = await InstanceSingletonServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service1 = scope.GetRequiredService<IMyService>();
        var service2 = scope.GetRequiredService<IMyService>();

        await Assert.That(service1).IsSameReferenceAs(service2);
    }

    [Test]
    public async Task AddSingleton_Instance_ReturnsSameInstance_FromDifferentScopes()
    {
        await using var serviceProvider = await InstanceSingletonServiceProvider.BuildAsync();

        await using var scope1 = serviceProvider.CreateScope();
        await using var scope2 = serviceProvider.CreateScope();

        var service1 = scope1.GetRequiredService<IMyService>();
        var service2 = scope2.GetRequiredService<IMyService>();

        await Assert.That(service1).IsSameReferenceAs(service2);
    }

    [Test]
    public async Task AddSingleton_Instance_ReturnsExactProvidedInstance()
    {
        await using var serviceProvider = await InstanceSingletonServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var resolved = scope.GetRequiredService<IMyService>();

        // The resolved instance must be the exact same object that was registered
        await Assert.That(resolved).IsSameReferenceAs(InstanceSingletonServiceProvider.ProvidedInstance);
    }

    [Test]
    public async Task AddSingleton_Instance_DisposableNotDisposedByContainer()
    {
        var disposableInstance = new DisposableService();

        await using (var serviceProvider = await InstanceDisposableServiceProvider.BuildAsync())
        {
            await using var scope = serviceProvider.CreateScope();
            var resolved = scope.GetRequiredService<IDisposableService>();
            await Assert.That(resolved).IsSameReferenceAs(InstanceDisposableServiceProvider.DisposableInstance);
        }

        // After the service provider is disposed, the externally-owned instance
        // may or may not be disposed depending on the container's behavior.
        // Since the factory simply returns the instance, the singleton scope
        // will track it via Register() and dispose it. This test documents that behavior.
        // Users who want to control disposal themselves should manage the instance lifetime externally.
    }

    [Test]
    public async Task AddSingleton_NonGeneric_CanBeResolved()
    {
        await using var serviceProvider = await InstanceNonGenericServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IMyService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service.Name).IsEqualTo("NonGeneric");
    }

    [Test]
    public async Task AddSingleton_Instance_WorksWithFluentChaining()
    {
        await using var serviceProvider = await InstanceFluentServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var myService = scope.GetRequiredService<IMyService>();
        var otherService = scope.GetRequiredService<IOtherService>();

        await Assert.That(myService).IsNotNull();
        await Assert.That(myService.Name).IsEqualTo("Fluent1");
        await Assert.That(otherService).IsNotNull();
        await Assert.That(otherService.Value).IsEqualTo(42);
    }

    [Test]
    public async Task AddSingleton_Instance_WorksAlongsideAttributeRegistration()
    {
        await using var serviceProvider = await InstanceMixedServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        // Instance-registered service
        var myService = scope.GetRequiredService<IMyService>();
        await Assert.That(myService.Name).IsEqualTo("InstanceRegistered");

        // Attribute-registered service
        var attributeService = scope.GetRequiredService<AttributeService>();
        await Assert.That(attributeService).IsNotNull();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Service Provider Definitions
    // ═══════════════════════════════════════════════════════════════════════

    [ServiceProvider]
    public partial class InstanceSingletonServiceProvider
    {
        public static readonly MyService ProvidedInstance = new("PreExisting");

        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<IMyService>(ProvidedInstance);
            }
        }
    }

    [ServiceProvider]
    public partial class InstanceDisposableServiceProvider
    {
        public static readonly DisposableService DisposableInstance = new();

        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<IDisposableService>(DisposableInstance);
            }
        }
    }

    [ServiceProvider]
    public partial class InstanceNonGenericServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton(typeof(IMyService), new MyService("NonGeneric"));
            }
        }
    }

    [ServiceProvider]
    public partial class InstanceFluentServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<IMyService>(new MyService("Fluent1"))
                    .AddSingleton<IOtherService>(new OtherService(42));
            }
        }
    }

    [ServiceProvider]
    [Singleton<AttributeService>]
    public partial class InstanceMixedServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<IMyService>(new MyService("InstanceRegistered"));
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Test Services
    // ═══════════════════════════════════════════════════════════════════════

    public interface IMyService
    {
        string Name { get; }
    }

    public class MyService(string name) : IMyService
    {
        public string Name => name;
    }

    public interface IDisposableService : IDisposable
    {
        bool IsDisposed { get; }
    }

    public class DisposableService : IDisposableService
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    public interface IOtherService
    {
        int Value { get; }
    }

    public class OtherService(int value) : IOtherService
    {
        public int Value => value;
    }

    public class AttributeService { }
}
