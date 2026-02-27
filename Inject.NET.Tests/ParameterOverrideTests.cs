using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.Models;

namespace Inject.NET.Tests;

public partial class ParameterOverrideTests
{
    [Test]
    public async Task TypedParameter_OverridesConstructorParameterByType()
    {
        await using var serviceProvider = await ParameterOverrideServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.Resolve<ServiceWithConnectionString>(
            new TypedParameter<string>("Server=custom"));

        await Assert.That(service).IsNotNull();
        await Assert.That(service.ConnectionString).IsEqualTo("Server=custom");
    }

    [Test]
    public async Task NamedParameter_OverridesConstructorParameterByName()
    {
        await using var serviceProvider = await ParameterOverrideServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.Resolve<ServiceWithConnectionString>(
            new NamedParameter("connectionString", "Server=named"));

        await Assert.That(service).IsNotNull();
        await Assert.That(service.ConnectionString).IsEqualTo("Server=named");
    }

    [Test]
    public async Task NonOverriddenParameters_ResolvedFromContainer()
    {
        await using var serviceProvider = await ParameterOverrideServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.Resolve<ServiceWithMixedDependencies>(
            new NamedParameter("connectionString", "Server=mixed"));

        await Assert.That(service).IsNotNull();
        await Assert.That(service.ConnectionString).IsEqualTo("Server=mixed");
        // The ILogger dependency should have been resolved from the container
        await Assert.That(service.Logger).IsNotNull();
        await Assert.That(service.Logger).IsTypeOf<ConsoleLogger>();
    }

    [Test]
    public async Task MultipleParameters_AllApplied()
    {
        await using var serviceProvider = await ParameterOverrideServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.Resolve<ServiceWithMultipleParams>(
            new NamedParameter("name", "TestService"),
            new TypedParameter<int>(42));

        await Assert.That(service).IsNotNull();
        await Assert.That(service.Name).IsEqualTo("TestService");
        await Assert.That(service.Value).IsEqualTo(42);
        // The ILogger dependency should have been resolved from the container
        await Assert.That(service.Logger).IsNotNull();
    }

    [Test]
    public async Task TypedParameter_OverridesIntParameter()
    {
        await using var serviceProvider = await ParameterOverrideServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.Resolve<ServiceWithIntParam>(
            new TypedParameter<int>(99));

        await Assert.That(service).IsNotNull();
        await Assert.That(service.Count).IsEqualTo(99);
    }

    [Test]
    public async Task NamedParameter_DistinguishesBetweenSameTypeParameters()
    {
        await using var serviceProvider = await ParameterOverrideServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.Resolve<ServiceWithTwoStrings>(
            new NamedParameter("first", "Hello"),
            new NamedParameter("second", "World"));

        await Assert.That(service).IsNotNull();
        await Assert.That(service.First).IsEqualTo("Hello");
        await Assert.That(service.Second).IsEqualTo("World");
    }

    [Test]
    public async Task Resolve_WithNoParameters_ResolvesAllFromContainer()
    {
        await using var serviceProvider = await ParameterOverrideServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.Resolve<ServiceWithOnlyContainerDeps>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service.Logger).IsNotNull();
        await Assert.That(service.Logger).IsTypeOf<ConsoleLogger>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Service Provider Definition
    // ═══════════════════════════════════════════════════════════════════════

    [ServiceProvider]
    [Singleton<ILogger, ConsoleLogger>]
    public partial class ParameterOverrideServiceProvider;

    // ═══════════════════════════════════════════════════════════════════════
    // Test Services
    // ═══════════════════════════════════════════════════════════════════════

    public interface ILogger
    {
        string Name { get; }
    }

    public class ConsoleLogger : ILogger
    {
        public string Name => "ConsoleLogger";
    }

    public class ServiceWithConnectionString
    {
        public string ConnectionString { get; }

        public ServiceWithConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }

    public class ServiceWithMixedDependencies
    {
        public string ConnectionString { get; }
        public ILogger Logger { get; }

        public ServiceWithMixedDependencies(ILogger logger, string connectionString)
        {
            Logger = logger;
            ConnectionString = connectionString;
        }
    }

    public class ServiceWithMultipleParams
    {
        public string Name { get; }
        public int Value { get; }
        public ILogger Logger { get; }

        public ServiceWithMultipleParams(ILogger logger, string name, int value)
        {
            Logger = logger;
            Name = name;
            Value = value;
        }
    }

    public class ServiceWithIntParam
    {
        public int Count { get; }

        public ServiceWithIntParam(int count)
        {
            Count = count;
        }
    }

    public class ServiceWithTwoStrings
    {
        public string First { get; }
        public string Second { get; }

        public ServiceWithTwoStrings(string first, string second)
        {
            First = first;
            Second = second;
        }
    }

    public class ServiceWithOnlyContainerDeps
    {
        public ILogger Logger { get; }

        public ServiceWithOnlyContainerDeps(ILogger logger)
        {
            Logger = logger;
        }
    }
}
