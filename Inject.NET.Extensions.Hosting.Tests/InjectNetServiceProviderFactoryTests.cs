using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.Extensions.DependencyInjection;
using Inject.NET.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Inject.NET.Extensions.Hosting.Tests;

public partial class InjectNetServiceProviderFactoryTests
{
    public interface ICompileTimeService
    {
        string Name { get; }
    }

    public class CompileTimeServiceImpl : ICompileTimeService
    {
        public string Name => "CompileTime";
    }

    public interface IRuntimeService
    {
        string Value { get; }
    }

    public class RuntimeServiceImpl : IRuntimeService
    {
        public string Value => "Runtime";
    }

    [ServiceProvider]
    [Singleton<ICompileTimeService, CompileTimeServiceImpl>]
    public partial class FactoryTestProvider;

    [Test]
    public async Task Factory_CreatesChildContainer_FromPreBuiltProvider()
    {
        await using var provider = await FactoryTestProvider.BuildAsync();
        var factory = new InjectNetServiceProviderFactory(provider);

        var services = new ServiceCollection();
        var builder = factory.CreateBuilder(services);
        var sp = factory.CreateServiceProvider(builder);

        await Assert.That(sp).IsNotNull();
    }

    [Test]
    public async Task HostMediServices_ResolvableThroughChildContainer()
    {
        await using var provider = await FactoryTestProvider.BuildAsync();
        var factory = new InjectNetServiceProviderFactory(provider);

        var services = new ServiceCollection();
        services.AddSingleton<IRuntimeService, RuntimeServiceImpl>();

        var builder = factory.CreateBuilder(services);
        var sp = factory.CreateServiceProvider(builder);

        var runtime = sp.GetService(typeof(IRuntimeService)) as IRuntimeService;

        await Assert.That(runtime).IsNotNull();
        await Assert.That(runtime!.Value).IsEqualTo("Runtime");
    }

    [Test]
    public async Task CompileTimeServices_StillResolvableFromChildContainer()
    {
        await using var provider = await FactoryTestProvider.BuildAsync();
        var factory = new InjectNetServiceProviderFactory(provider);

        var services = new ServiceCollection();
        var builder = factory.CreateBuilder(services);
        var sp = factory.CreateServiceProvider(builder);

        var compileTime = sp.GetService(typeof(ICompileTimeService)) as ICompileTimeService;

        await Assert.That(compileTime).IsNotNull();
        await Assert.That(compileTime!.Name).IsEqualTo("CompileTime");
    }

    [Test]
    public async Task ServiceScopeFactory_WorksCorrectly()
    {
        await using var provider = await FactoryTestProvider.BuildAsync();
        var factory = new InjectNetServiceProviderFactory(provider);

        var services = new ServiceCollection();
        services.AddScoped<RuntimeServiceImpl>();

        var builder = factory.CreateBuilder(services);
        var sp = factory.CreateServiceProvider(builder);

        var scopeFactory = sp.GetService(typeof(IServiceScopeFactory)) as IServiceScopeFactory;
        await Assert.That(scopeFactory).IsNotNull();

        using var scope = scopeFactory!.CreateScope();
        var svc = scope.ServiceProvider.GetService(typeof(RuntimeServiceImpl));
        await Assert.That(svc).IsNotNull();
    }

    [Test]
    public async Task ServiceProviderIsService_WorksCorrectly()
    {
        await using var provider = await FactoryTestProvider.BuildAsync();
        var factory = new InjectNetServiceProviderFactory(provider);

        var services = new ServiceCollection();
        services.AddSingleton<IRuntimeService, RuntimeServiceImpl>();

        var builder = factory.CreateBuilder(services);
        var sp = factory.CreateServiceProvider(builder);

        var isService = sp.GetService(typeof(IServiceProviderIsService)) as IServiceProviderIsService;
        await Assert.That(isService).IsNotNull();
        await Assert.That(isService!.IsService(typeof(IRuntimeService))).IsTrue();
        await Assert.That(isService.IsService(typeof(ICompileTimeService))).IsTrue();
    }

    [Test]
    public async Task ChildContainerDisposal_DoesNotAffectParent()
    {
        await using var provider = await FactoryTestProvider.BuildAsync();
        var factory = new InjectNetServiceProviderFactory(provider);

        var services = new ServiceCollection();
        var builder = factory.CreateBuilder(services);
        var child = builder.Build();

        // Dispose child
        await child.DisposeAsync();

        // Parent should still work
        await using var scope = provider.CreateScope();
        var compileTime = scope.GetRequiredService<ICompileTimeService>();
        await Assert.That(compileTime).IsNotNull();
    }
}
