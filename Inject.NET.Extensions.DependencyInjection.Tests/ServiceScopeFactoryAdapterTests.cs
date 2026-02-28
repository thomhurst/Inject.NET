using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Inject.NET.Extensions.DependencyInjection.Tests;

public partial class ServiceScopeFactoryAdapterTests
{
    public class MyScopedService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public class MySingletonService;

    [ServiceProvider]
    [Singleton<MySingletonService>]
    [Scoped<MyScopedService>]
    public partial class ScopeFactoryTestProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddServiceCollection(_ => { });
            }
        }
    }

    [Test]
    public async Task IServiceScopeFactory_CanBeResolvedFromScope()
    {
        await using var serviceProvider = await ScopeFactoryTestProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var factory = scope.GetRequiredService<IServiceScopeFactory>();

        await Assert.That(factory).IsNotNull();
    }

    [Test]
    public async Task CreateScope_ReturnsWorkingMediScope()
    {
        await using var serviceProvider = await ScopeFactoryTestProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var factory = scope.GetRequiredService<IServiceScopeFactory>();

        using var mediScope = factory.CreateScope();

        await Assert.That(mediScope).IsNotNull();
        await Assert.That(mediScope.ServiceProvider).IsNotNull();

        var service = mediScope.ServiceProvider.GetService(typeof(MySingletonService));
        await Assert.That(service).IsNotNull();
    }

    [Test]
    public async Task ScopedServices_AreSameWithinScope()
    {
        await using var serviceProvider = await ScopeFactoryTestProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var factory = scope.GetRequiredService<IServiceScopeFactory>();

        using var mediScope = factory.CreateScope();

        var service1 = mediScope.ServiceProvider.GetService(typeof(MyScopedService)) as MyScopedService;
        var service2 = mediScope.ServiceProvider.GetService(typeof(MyScopedService)) as MyScopedService;

        await Assert.That(service1).IsNotNull();
        await Assert.That(service2).IsNotNull();
        await Assert.That(service1!.Id).IsEqualTo(service2!.Id);
    }

    [Test]
    public async Task ScopedServices_AreDifferentAcrossScopes()
    {
        await using var serviceProvider = await ScopeFactoryTestProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var factory = scope.GetRequiredService<IServiceScopeFactory>();

        using var mediScope1 = factory.CreateScope();
        using var mediScope2 = factory.CreateScope();

        var service1 = mediScope1.ServiceProvider.GetService(typeof(MyScopedService)) as MyScopedService;
        var service2 = mediScope2.ServiceProvider.GetService(typeof(MyScopedService)) as MyScopedService;

        await Assert.That(service1).IsNotNull();
        await Assert.That(service2).IsNotNull();
        await Assert.That(service1!.Id).IsNotEqualTo(service2!.Id);
    }
}
