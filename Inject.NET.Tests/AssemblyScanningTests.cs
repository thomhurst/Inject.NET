using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

/// <summary>
/// Tests for assembly scanning and convention-based service registration.
/// Verifies that the Scan() extension method correctly discovers and registers
/// services from assemblies based on various conventions.
/// </summary>
public partial class AssemblyScanningTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // AddAllTypesOf Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Scan_AddAllTypesOf_RegistersAllImplementations()
    {
        await using var serviceProvider = await ScanAllTypesServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var handlerList = scope.GetServices<IHandler>().ToList();

        await Assert.That(handlerList).HasCount().EqualTo(2);
        await Assert.That(handlerList.OfType<HandlerA>().Count()).IsEqualTo(1);
        await Assert.That(handlerList.OfType<HandlerB>().Count()).IsEqualTo(1);
    }

    [Test]
    public async Task Scan_AddAllTypesOf_DoesNotRegisterAbstractClasses()
    {
        await using var serviceProvider = await ScanWithAbstractServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var serviceList = scope.GetServices<IBaseService>().ToList();

        // Should only register ConcreteServiceA, not AbstractBaseService
        await Assert.That(serviceList).HasCount().EqualTo(1);
        await Assert.That(serviceList[0]).IsTypeOf<ConcreteServiceA>();
    }

    [Test]
    public async Task Scan_AddAllTypesOf_DoesNotRegisterInterfaces()
    {
        await using var serviceProvider = await ScanInterfacesOnlyServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var serviceList = scope.GetServices<IMarkerInterface>().ToList();

        // Should only register ConcreteMarked, not IExtendedMarker (which is an interface)
        await Assert.That(serviceList).HasCount().EqualTo(1);
        await Assert.That(serviceList[0]).IsTypeOf<ConcreteMarked>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // WithDefaultConventions Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Scan_WithDefaultConventions_RegistersMatchingTypes()
    {
        await using var serviceProvider = await DefaultConventionsServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var notification = scope.GetRequiredService<INotificationService>();

        await Assert.That(notification).IsNotNull();
        await Assert.That(notification).IsTypeOf<NotificationService>();
    }

    [Test]
    public async Task Scan_WithDefaultConventions_DoesNotRegisterNonMatchingTypes()
    {
        await using var serviceProvider = await DefaultConventionsNonMatchServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        // IWidgetService has no matching "WidgetService" class - only CustomWidget exists
        // So it should not be registered
        var widgetList = scope.GetServices<IWidgetService>().ToList();

        await Assert.That(widgetList).HasCount().EqualTo(0);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Lifetime Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Scan_AsSingleton_RegistersWithSingletonLifetime()
    {
        await using var serviceProvider = await ScanSingletonServiceProvider.BuildAsync();

        await using var scope1 = serviceProvider.CreateScope();
        var service1 = scope1.GetRequiredService<ILifetimeService>();

        await using var scope2 = serviceProvider.CreateScope();
        var service2 = scope2.GetRequiredService<ILifetimeService>();

        await Assert.That(service1).IsSameReferenceAs(service2);
    }

    [Test]
    public async Task Scan_AsScoped_RegistersWithScopedLifetime()
    {
        await using var serviceProvider = await ScanScopedServiceProvider.BuildAsync();

        await using var scope1 = serviceProvider.CreateScope();
        var service1a = scope1.GetRequiredService<ILifetimeService>();
        var service1b = scope1.GetRequiredService<ILifetimeService>();

        await using var scope2 = serviceProvider.CreateScope();
        var service2 = scope2.GetRequiredService<ILifetimeService>();

        // Same within scope
        await Assert.That(service1a).IsSameReferenceAs(service1b);
        // Different across scopes
        await Assert.That(service1a).IsNotSameReferenceAs(service2);
    }

    [Test]
    public async Task Scan_AsTransient_RegistersWithTransientLifetime()
    {
        await using var serviceProvider = await ScanTransientServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service1 = scope.GetRequiredService<ILifetimeService>();
        var service2 = scope.GetRequiredService<ILifetimeService>();

        await Assert.That(service1).IsNotSameReferenceAs(service2);
    }

    [Test]
    public async Task Scan_DefaultLifetime_IsTransient()
    {
        await using var serviceProvider = await ScanDefaultLifetimeServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service1 = scope.GetRequiredService<ILifetimeService>();
        var service2 = scope.GetRequiredService<ILifetimeService>();

        // Default lifetime is transient, so instances should be different
        await Assert.That(service1).IsNotSameReferenceAs(service2);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Multiple Assembly Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Scan_MultipleAssemblies_ScansAll()
    {
        await using var serviceProvider = await MultiAssemblyServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var handlerList = scope.GetServices<IHandler>().ToList();

        // FromAssembly is called twice with the same assembly, so implementations are registered twice
        await Assert.That(handlerList.Count).IsGreaterThanOrEqualTo(2);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Combined Features Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Scan_CombinedAddAllTypesOfAndDefaultConventions_BothWork()
    {
        await using var serviceProvider = await CombinedScanServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        // AddAllTypesOf<IHandler> should register HandlerA and HandlerB
        var handlerList = scope.GetServices<IHandler>().ToList();
        await Assert.That(handlerList).HasCount().EqualTo(2);

        // WithDefaultConventions should register INotificationService -> NotificationService
        var notification = scope.GetRequiredService<INotificationService>();
        await Assert.That(notification).IsTypeOf<NotificationService>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MatchesDefaultConvention Unit Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task MatchesDefaultConvention_IFooAndFoo_ReturnsTrue()
    {
        var result = AssemblyScanner.MatchesDefaultConvention(typeof(IFoo), typeof(Foo));
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task MatchesDefaultConvention_IFooAndBar_ReturnsFalse()
    {
        var result = AssemblyScanner.MatchesDefaultConvention(typeof(IFoo), typeof(Bar));
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task MatchesDefaultConvention_NonStandardInterface_ReturnsFalse()
    {
        // Interface name "Ifoo" (lowercase 'f') should not match convention
        var result = AssemblyScanner.MatchesDefaultConvention(typeof(Ifoo), typeof(Foo));
        await Assert.That(result).IsFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Service Provider Definitions
    // ═══════════════════════════════════════════════════════════════════════

    [ServiceProvider]
    public partial class ScanAllTypesServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.Scan(scanner =>
                {
                    scanner.FromAssemblyOf<HandlerA>();
                    scanner.AddAllTypesOf<IHandler>();
                });
            }
        }
    }

    [ServiceProvider]
    public partial class ScanWithAbstractServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.Scan(scanner =>
                {
                    scanner.FromAssemblyOf<ConcreteServiceA>();
                    scanner.AddAllTypesOf<IBaseService>();
                });
            }
        }
    }

    [ServiceProvider]
    public partial class ScanInterfacesOnlyServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.Scan(scanner =>
                {
                    scanner.FromAssemblyOf<ConcreteMarked>();
                    scanner.AddAllTypesOf<IMarkerInterface>();
                });
            }
        }
    }

    [ServiceProvider]
    public partial class DefaultConventionsServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.Scan(scanner =>
                {
                    scanner.FromAssemblyOf<NotificationService>();
                    scanner.WithDefaultConventions();
                });
            }
        }
    }

    [ServiceProvider]
    public partial class DefaultConventionsNonMatchServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.Scan(scanner =>
                {
                    scanner.FromAssemblyOf<CustomWidget>();
                    scanner.WithDefaultConventions();
                });
            }
        }
    }

    [ServiceProvider]
    public partial class ScanSingletonServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.Scan(scanner =>
                {
                    scanner.FromAssemblyOf<LifetimeService>();
                    scanner.AddAllTypesOf<ILifetimeService>();
                    scanner.AsSingleton();
                });
            }
        }
    }

    [ServiceProvider]
    public partial class ScanScopedServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.Scan(scanner =>
                {
                    scanner.FromAssemblyOf<LifetimeService>();
                    scanner.AddAllTypesOf<ILifetimeService>();
                    scanner.AsScoped();
                });
            }
        }
    }

    [ServiceProvider]
    public partial class ScanTransientServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.Scan(scanner =>
                {
                    scanner.FromAssemblyOf<LifetimeService>();
                    scanner.AddAllTypesOf<ILifetimeService>();
                    scanner.AsTransient();
                });
            }
        }
    }

    [ServiceProvider]
    public partial class ScanDefaultLifetimeServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.Scan(scanner =>
                {
                    scanner.FromAssemblyOf<LifetimeService>();
                    scanner.AddAllTypesOf<ILifetimeService>();
                    // No lifetime specified - should default to transient
                });
            }
        }
    }

    [ServiceProvider]
    public partial class MultiAssemblyServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.Scan(scanner =>
                {
                    scanner.FromAssemblyOf<HandlerA>();
                    scanner.FromAssembly(typeof(HandlerA).Assembly);
                    scanner.AddAllTypesOf<IHandler>();
                });
            }
        }
    }

    [ServiceProvider]
    public partial class CombinedScanServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.Scan(scanner =>
                {
                    scanner.FromAssemblyOf<HandlerA>();
                    scanner.AddAllTypesOf<IHandler>();
                    scanner.WithDefaultConventions();
                });
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Test Services
    // ═══════════════════════════════════════════════════════════════════════

    // --- Handlers for AddAllTypesOf tests ---
    public interface IHandler { }
    public class HandlerA : IHandler { }
    public class HandlerB : IHandler { }

    // --- Abstract class filtering tests ---
    public interface IBaseService { }
    public abstract class AbstractBaseService : IBaseService { }
    public class ConcreteServiceA : IBaseService { }

    // --- Interface filtering tests ---
    public interface IMarkerInterface { }
    public interface IExtendedMarker : IMarkerInterface { }
    public class ConcreteMarked : IMarkerInterface { }

    // --- Default conventions tests ---
    public interface INotificationService { }
    public class NotificationService : INotificationService { }

    // --- Non-matching convention tests ---
    public interface IWidgetService { }
    public class CustomWidget : IWidgetService { } // Name doesn't match convention (not "WidgetService")

    // --- Lifetime tests ---
    public interface ILifetimeService { }
    public class LifetimeService : ILifetimeService { }

    // --- Convention matching unit test types ---
    public interface IFoo { }
    public class Foo : IFoo { }
    public class Bar : IFoo { }

    // Interface with non-standard naming (lowercase after 'I')
    // ReSharper disable once InconsistentNaming
    public interface Ifoo { }
}
