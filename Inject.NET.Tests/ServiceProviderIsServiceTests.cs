using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Inject.NET.Tests;

public partial class ServiceProviderIsServiceTests
{
    [Test]
    public async Task IsService_ReturnsTrueForRegisteredSingleton()
    {
        await using var serviceProvider = await IsServiceTestProvider.BuildAsync();

        var checker = (IServiceProviderIsService)serviceProvider;

        await Assert.That(checker.IsService(typeof(SingletonService))).IsTrue();
    }

    [Test]
    public async Task IsService_ReturnsTrueForRegisteredScoped()
    {
        await using var serviceProvider = await IsServiceTestProvider.BuildAsync();

        var checker = (IServiceProviderIsService)serviceProvider;

        await Assert.That(checker.IsService(typeof(ScopedService))).IsTrue();
    }

    [Test]
    public async Task IsService_ReturnsTrueForRegisteredTransient()
    {
        await using var serviceProvider = await IsServiceTestProvider.BuildAsync();

        var checker = (IServiceProviderIsService)serviceProvider;

        await Assert.That(checker.IsService(typeof(TransientService))).IsTrue();
    }

    [Test]
    public async Task IsService_ReturnsTrueForRegisteredInterface()
    {
        await using var serviceProvider = await IsServiceTestProvider.BuildAsync();

        var checker = (IServiceProviderIsService)serviceProvider;

        await Assert.That(checker.IsService(typeof(IMyService))).IsTrue();
    }

    [Test]
    public async Task IsService_ReturnsFalseForUnregisteredType()
    {
        await using var serviceProvider = await IsServiceTestProvider.BuildAsync();

        var checker = (IServiceProviderIsService)serviceProvider;

        await Assert.That(checker.IsService(typeof(UnregisteredService))).IsFalse();
    }

    [Test]
    public async Task IsService_ReturnsTrueForIServiceProviderIsService()
    {
        await using var serviceProvider = await IsServiceTestProvider.BuildAsync();

        var checker = (IServiceProviderIsService)serviceProvider;

        await Assert.That(checker.IsService(typeof(IServiceProviderIsService))).IsTrue();
    }

    [Test]
    public async Task IsService_ReturnsTrueForIServiceProvider()
    {
        await using var serviceProvider = await IsServiceTestProvider.BuildAsync();

        var checker = (IServiceProviderIsService)serviceProvider;

        await Assert.That(checker.IsService(typeof(System.IServiceProvider))).IsTrue();
    }

    [Test]
    public async Task IsService_CanBeResolvedFromScope()
    {
        await using var serviceProvider = await IsServiceTestProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var checker = scope.GetRequiredService<IServiceProviderIsService>();

        await Assert.That(checker).IsNotNull();
        await Assert.That(checker.IsService(typeof(SingletonService))).IsTrue();
        await Assert.That(checker.IsService(typeof(UnregisteredService))).IsFalse();
    }

    [Test]
    public async Task IsService_ReturnsTrueForOpenGenericRegistration()
    {
        await using var serviceProvider = await IsServiceWithGenericsTestProvider.BuildAsync();

        var checker = (IServiceProviderIsService)serviceProvider;

        await Assert.That(checker.IsService(typeof(IGenericService<string>))).IsTrue();
        await Assert.That(checker.IsService(typeof(IGenericService<int>))).IsTrue();
    }

    public interface IMyService;

    public class MyServiceImpl : IMyService;

    public class SingletonService;

    public class ScopedService;

    public class TransientService;

    public class UnregisteredService;

    public interface IGenericService<T>;

    public class GenericService<T> : IGenericService<T>;

    [ServiceProvider]
    [Singleton<SingletonService>]
    [Scoped<ScopedService>]
    [Transient<TransientService>]
    [Singleton<IMyService, MyServiceImpl>]
    public partial class IsServiceTestProvider;

    [ServiceProvider]
    [Singleton(typeof(IGenericService<>), typeof(GenericService<>))]
    public partial class IsServiceWithGenericsTestProvider;
}
