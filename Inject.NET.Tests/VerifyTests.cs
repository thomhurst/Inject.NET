using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class VerifyTests
{
    [Test]
    public async Task Verify_PassesForValidServiceProvider_WithSingletons()
    {
        await using var serviceProvider = await ValidSingletonServiceProvider.BuildAsync();

        await serviceProvider.Verify();
    }

    [Test]
    public async Task Verify_PassesForValidServiceProvider_WithScopedServices()
    {
        await using var serviceProvider = await ValidScopedServiceProvider.BuildAsync();

        await serviceProvider.Verify();
    }

    [Test]
    public async Task Verify_PassesForValidServiceProvider_WithTransientServices()
    {
        await using var serviceProvider = await ValidTransientServiceProvider.BuildAsync();

        await serviceProvider.Verify();
    }

    [Test]
    public async Task Verify_PassesForValidServiceProvider_WithMixedLifetimes()
    {
        await using var serviceProvider = await ValidMixedServiceProvider.BuildAsync();

        await serviceProvider.Verify();
    }

    [Test]
    public async Task Verify_PassesForValidServiceProvider_WithDependencyChain()
    {
        await using var serviceProvider = await ValidDependencyChainServiceProvider.BuildAsync();

        await serviceProvider.Verify();
    }

    [Test]
    public async Task Verify_PassesForValidServiceProvider_WithDecorators()
    {
        await using var serviceProvider = await ValidDecoratorServiceProvider.BuildAsync();

        await serviceProvider.Verify();
    }

    [Test]
    public async Task Verify_PassesForValidServiceProvider_WithExtensionMethodRegistrations()
    {
        await using var serviceProvider = await ValidExtensionMethodServiceProvider.BuildAsync();

        await serviceProvider.Verify();
    }

    [Test]
    public async Task Verify_ThrowsAggregateException_WhenServiceFactoryFails()
    {
        await using var serviceProvider = await FailingFactoryServiceProvider.BuildAsync();

        var exception = await Assert.ThrowsAsync<AggregateException>(
            () => serviceProvider.Verify());

        await Assert.That(exception.InnerExceptions.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(exception.Message).Contains("verification failed");
    }

    [Test]
    public async Task Verify_ThrowsAggregateException_WithDescriptiveMessage_WhenServiceFactoryFails()
    {
        await using var serviceProvider = await FailingFactoryServiceProvider.BuildAsync();

        var exception = await Assert.ThrowsAsync<AggregateException>(
            () => serviceProvider.Verify());

        // Check that at least one inner exception references the failing service type
        var hasDescriptiveMessage = false;
        foreach (var innerException in exception.InnerExceptions)
        {
            if (innerException.Message.Contains("IFailingService"))
            {
                hasDescriptiveMessage = true;
                break;
            }
        }

        await Assert.That(hasDescriptiveMessage).IsTrue();
    }

    [Test]
    public async Task Verify_ThrowsAggregateException_WhenFactoryReturnsNull()
    {
        await using var serviceProvider = await NullFactoryServiceProvider.BuildAsync();

        var exception = await Assert.ThrowsAsync<AggregateException>(
            () => serviceProvider.Verify());

        await Assert.That(exception.InnerExceptions.Count).IsGreaterThanOrEqualTo(1);

        var hasNullMessage = false;
        foreach (var innerException in exception.InnerExceptions)
        {
            if (innerException.Message.Contains("returned null"))
            {
                hasNullMessage = true;
                break;
            }
        }

        await Assert.That(hasNullMessage).IsTrue();
    }

    [Test]
    public async Task Verify_PassesForValidServiceProvider_WithTenants()
    {
        await using var serviceProvider = await ValidTenantServiceProvider.BuildAsync();

        await serviceProvider.Verify();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Valid Service Providers
    // ═══════════════════════════════════════════════════════════════════════

    [ServiceProvider]
    [Singleton<IVerifyService, VerifyServiceImpl>]
    public partial class ValidSingletonServiceProvider;

    [ServiceProvider]
    [Scoped<IVerifyService, VerifyServiceImpl>]
    public partial class ValidScopedServiceProvider;

    [ServiceProvider]
    [Transient<IVerifyService, VerifyServiceImpl>]
    public partial class ValidTransientServiceProvider;

    [ServiceProvider]
    [Singleton<IVerifyService, VerifyServiceImpl>]
    [Scoped<IVerifyScopedService, VerifyScopedServiceImpl>]
    [Transient<IVerifyTransientService, VerifyTransientServiceImpl>]
    public partial class ValidMixedServiceProvider;

    [ServiceProvider]
    [Singleton<IVerifyDependency, VerifyDependencyImpl>]
    [Scoped<IVerifyServiceWithDep, VerifyServiceWithDepImpl>]
    public partial class ValidDependencyChainServiceProvider;

    [ServiceProvider]
    [Singleton<IVerifyDecorable, VerifyDecorableImpl>]
    [SingletonDecorator<IVerifyDecorable, VerifyDecorator>]
    public partial class ValidDecoratorServiceProvider;

    [ServiceProvider]
    public partial class ValidExtensionMethodServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<IVerifyService, VerifyServiceImpl>()
                    .AddScoped<IVerifyScopedService, VerifyScopedServiceImpl>();
            }
        }
    }

    [ServiceProvider]
    [Singleton<IVerifyService, VerifyServiceImpl>]
    [WithTenant<VerifyTenant>]
    public partial class ValidTenantServiceProvider;

    public record VerifyTenant;

    // ═══════════════════════════════════════════════════════════════════════
    // Failing Service Providers
    // ═══════════════════════════════════════════════════════════════════════

    [ServiceProvider]
    public partial class FailingFactoryServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<IFailingService>(scope =>
                    throw new InvalidOperationException("Intentional failure for testing"));
            }
        }
    }

    [ServiceProvider]
    public partial class NullFactoryServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<INullService>(scope => null!);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Test Service Interfaces and Implementations
    // ═══════════════════════════════════════════════════════════════════════

    public interface IVerifyService { }
    public class VerifyServiceImpl : IVerifyService { }

    public interface IVerifyScopedService { }
    public class VerifyScopedServiceImpl : IVerifyScopedService { }

    public interface IVerifyTransientService { }
    public class VerifyTransientServiceImpl : IVerifyTransientService { }

    public interface IVerifyDependency { }
    public class VerifyDependencyImpl : IVerifyDependency { }

    public interface IVerifyServiceWithDep
    {
        IVerifyDependency Dependency { get; }
    }

    public class VerifyServiceWithDepImpl(IVerifyDependency dependency) : IVerifyServiceWithDep
    {
        public IVerifyDependency Dependency => dependency;
    }

    public interface IVerifyDecorable
    {
        string Name { get; }
    }

    public class VerifyDecorableImpl : IVerifyDecorable
    {
        public string Name => "Original";
    }

    public class VerifyDecorator(IVerifyDecorable inner) : IVerifyDecorable
    {
        public IVerifyDecorable Inner => inner;
        public string Name => $"Decorated({inner.Name})";
    }

    public interface IFailingService { }

    public interface INullService { }
}
