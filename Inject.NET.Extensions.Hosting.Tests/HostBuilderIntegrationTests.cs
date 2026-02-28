using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.Extensions.DependencyInjection;
using Inject.NET.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Inject.NET.Extensions.Hosting.Tests;

public partial class HostBuilderIntegrationTests
{
    public interface IGreeter
    {
        string Greet();
    }

    public class Greeter : IGreeter
    {
        public string Greet() => "Hello from Inject.NET";
    }

    public interface IClock
    {
        string Now();
    }

    public class SystemClock : IClock
    {
        public string Now() => "2026-01-01";
    }

    public class ScopedCounter
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    [ServiceProvider]
    [Singleton<IGreeter, Greeter>]
    public partial class HostTestProvider;

    [Test]
    public async Task UseInjectNet_RegistersFactory_OnHostBuilder()
    {
        await using var provider = await HostTestProvider.BuildAsync();

        // Verify the extension method doesn't throw and returns the builder
        var hostBuilder = new HostBuilder();
        var result = hostBuilder.UseInjectNet(provider);

        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task Factory_BothServiceTypes_Resolvable_ViaHostBuilder()
    {
        await using var provider = await HostTestProvider.BuildAsync();

        // Simulate what the host builder does internally:
        // 1. Collects services via ConfigureServices
        // 2. Calls factory.CreateBuilder(services)
        // 3. Calls factory.CreateServiceProvider(builder)
        var factory = new InjectNetServiceProviderFactory(provider);

        var services = new ServiceCollection();
        services.AddSingleton<IClock, SystemClock>();

        var builder = factory.CreateBuilder(services);
        var sp = factory.CreateServiceProvider(builder);

        // Compile-time service resolvable
        var greeter = sp.GetService(typeof(IGreeter)) as IGreeter;
        await Assert.That(greeter).IsNotNull();
        await Assert.That(greeter!.Greet()).IsEqualTo("Hello from Inject.NET");

        // Runtime MEDI service resolvable
        var clock = sp.GetService(typeof(IClock)) as IClock;
        await Assert.That(clock).IsNotNull();
        await Assert.That(clock!.Now()).IsEqualTo("2026-01-01");
    }

    [Test]
    public async Task Factory_Scoping_WorksCorrectly_ViaHostBuilder()
    {
        await using var provider = await HostTestProvider.BuildAsync();
        var factory = new InjectNetServiceProviderFactory(provider);

        var services = new ServiceCollection();
        services.AddScoped<ScopedCounter>();

        var builder = factory.CreateBuilder(services);
        var sp = factory.CreateServiceProvider(builder);

        var scopeFactory = sp.GetService(typeof(IServiceScopeFactory)) as IServiceScopeFactory;
        await Assert.That(scopeFactory).IsNotNull();

        using var scope1 = scopeFactory!.CreateScope();
        using var scope2 = scopeFactory.CreateScope();

        var counter1a = scope1.ServiceProvider.GetService(typeof(ScopedCounter)) as ScopedCounter;
        var counter1b = scope1.ServiceProvider.GetService(typeof(ScopedCounter)) as ScopedCounter;
        var counter2 = scope2.ServiceProvider.GetService(typeof(ScopedCounter)) as ScopedCounter;

        await Assert.That(counter1a).IsNotNull();
        await Assert.That(counter2).IsNotNull();

        // Same within scope
        await Assert.That(counter1a!.Id).IsEqualTo(counter1b!.Id);
        // Different across scopes
        await Assert.That(counter1a.Id).IsNotEqualTo(counter2!.Id);
    }
}
