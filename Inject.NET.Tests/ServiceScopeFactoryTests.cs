using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Inject.NET.Tests;

public partial class ServiceScopeFactoryTests
{
    [Test]
    public async Task ServiceProvider_ImplementsIServiceScopeFactory()
    {
        await using var serviceProvider = await ScopeFactoryTestProvider.BuildAsync();

        await Assert.That(serviceProvider).IsAssignableTo<IServiceScopeFactory>();
    }

    [Test]
    public async Task IServiceScopeFactory_CreateScope_ReturnsServiceScope()
    {
        await using var serviceProvider = await ScopeFactoryTestProvider.BuildAsync();

        var factory = (IServiceScopeFactory)serviceProvider;

        using var msScope = factory.CreateScope();

        await Assert.That(msScope).IsNotNull();
        await Assert.That(msScope.ServiceProvider).IsNotNull();
    }

    [Test]
    public async Task IServiceScopeFactory_CreateScope_CanResolveRegisteredService()
    {
        await using var serviceProvider = await ScopeFactoryTestProvider.BuildAsync();

        var factory = (IServiceScopeFactory)serviceProvider;

        using var msScope = factory.CreateScope();

        var service = msScope.ServiceProvider.GetService(typeof(ScopeFactoryTestService));

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsAssignableTo<ScopeFactoryTestService>();
    }

    [Test]
    public async Task IServiceScopeFactory_CreateScope_ScopedServicesAreSameWithinScope()
    {
        await using var serviceProvider = await ScopeFactoryTestProvider.BuildAsync();

        var factory = (IServiceScopeFactory)serviceProvider;

        using var msScope = factory.CreateScope();

        var service1 = msScope.ServiceProvider.GetService(typeof(ScopeFactoryScopedService)) as ScopeFactoryScopedService;
        var service2 = msScope.ServiceProvider.GetService(typeof(ScopeFactoryScopedService)) as ScopeFactoryScopedService;

        await Assert.That(service1).IsNotNull();
        await Assert.That(service2).IsNotNull();
        await Assert.That(service1!.Id).IsEqualTo(service2!.Id);
    }

    [Test]
    public async Task IServiceScopeFactory_CreateScope_ScopedServicesAreDifferentAcrossScopes()
    {
        await using var serviceProvider = await ScopeFactoryTestProvider.BuildAsync();

        var factory = (IServiceScopeFactory)serviceProvider;

        using var msScope1 = factory.CreateScope();
        using var msScope2 = factory.CreateScope();

        var service1 = msScope1.ServiceProvider.GetService(typeof(ScopeFactoryScopedService)) as ScopeFactoryScopedService;
        var service2 = msScope2.ServiceProvider.GetService(typeof(ScopeFactoryScopedService)) as ScopeFactoryScopedService;

        await Assert.That(service1).IsNotNull();
        await Assert.That(service2).IsNotNull();
        await Assert.That(service1!.Id).IsNotEqualTo(service2!.Id);
    }

    [Test]
    public async Task IServiceScopeFactory_CanBeResolvedFromScope()
    {
        await using var serviceProvider = await ScopeFactoryTestProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var factory = scope.GetRequiredService<IServiceScopeFactory>();

        await Assert.That(factory).IsNotNull();
        await Assert.That(factory).IsAssignableTo<IServiceScopeFactory>();
    }

    [Test]
    public async Task IServiceScopeFactory_ResolvedFromScope_CanCreateNewScope()
    {
        await using var serviceProvider = await ScopeFactoryTestProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var factory = scope.GetRequiredService<IServiceScopeFactory>();

        using var msScope = factory.CreateScope();

        var service = msScope.ServiceProvider.GetService(typeof(ScopeFactoryTestService));

        await Assert.That(service).IsNotNull();
    }

    [Test]
    public async Task IsService_ReturnsTrueForIServiceScopeFactory()
    {
        await using var serviceProvider = await ScopeFactoryTestProvider.BuildAsync();

        var checker = (IServiceProviderIsService)serviceProvider;

        await Assert.That(checker.IsService(typeof(IServiceScopeFactory))).IsTrue();
    }

    public class ScopeFactoryTestService;

    public class ScopeFactoryScopedService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    [ServiceProvider]
    [Singleton<ScopeFactoryTestService>]
    [Scoped<ScopeFactoryScopedService>]
    public partial class ScopeFactoryTestProvider;
}
