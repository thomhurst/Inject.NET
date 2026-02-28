using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class FactoryMethodTests
{
    // === Shared types ===

    public interface IGreeter
    {
        string Greet();
    }

    public class Greeter : IGreeter
    {
        private readonly string _greeting;

        public Greeter(string greeting)
        {
            _greeting = greeting;
        }

        public string Greet() => _greeting;
    }

    public interface IDependency
    {
        string Value { get; }
    }

    public class MyDependency : IDependency
    {
        public string Value => "resolved-dependency";
    }

    public class ServiceWithDependency : IGreeter
    {
        private readonly IDependency _dep;
        private readonly string _extra;

        public ServiceWithDependency(IDependency dep, string extra)
        {
            _dep = dep;
            _extra = extra;
        }

        public string Greet() => $"{_dep.Value}:{_extra}";
    }

    // === Test 1: Singleton with factory method ===

    [ServiceProvider]
    [Singleton<IGreeter, Greeter>(FactoryMethod = nameof(CreateGreeter))]
    public partial class SingletonFactoryServiceProvider
    {
        public static Greeter CreateGreeter() => new Greeter("Hello from factory!");
    }

    [Test]
    public async Task SingletonFactoryMethod_UsesFactoryToCreateInstance()
    {
        await using var serviceProvider = await SingletonFactoryServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var greeter = scope.GetRequiredService<IGreeter>();

        await Assert.That(greeter.Greet()).IsEqualTo("Hello from factory!");
    }

    [Test]
    public async Task SingletonFactoryMethod_ReturnsSameInstanceAcrossScopes()
    {
        await using var serviceProvider = await SingletonFactoryServiceProvider.BuildAsync();
        await using var scope1 = serviceProvider.CreateScope();
        await using var scope2 = serviceProvider.CreateScope();

        var a = scope1.GetRequiredService<IGreeter>();
        var b = scope2.GetRequiredService<IGreeter>();

        await Assert.That(a).IsSameReferenceAs(b);
    }

    // === Test 2: Scoped with factory method + dependency resolution ===

    [ServiceProvider]
    [Singleton<IDependency, MyDependency>]
    [Scoped<IGreeter, ServiceWithDependency>(FactoryMethod = nameof(CreateServiceWithDep))]
    public partial class ScopedFactoryServiceProvider
    {
        public static ServiceWithDependency CreateServiceWithDep(IDependency dep)
            => new ServiceWithDependency(dep, "factory-extra");
    }

    [Test]
    public async Task ScopedFactoryMethod_ResolvesParametersFromContainer()
    {
        await using var serviceProvider = await ScopedFactoryServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var greeter = scope.GetRequiredService<IGreeter>();

        await Assert.That(greeter.Greet()).IsEqualTo("resolved-dependency:factory-extra");
    }

    // === Test 3: Transient with factory method ===

    [ServiceProvider]
    [Transient<IGreeter, Greeter>(FactoryMethod = nameof(CreateTransientGreeter))]
    public partial class TransientFactoryServiceProvider
    {
        public static Greeter CreateTransientGreeter() => new Greeter($"transient-{Guid.NewGuid():N}");
    }

    [Test]
    public async Task TransientFactoryMethod_CreatesNewInstanceEachTime()
    {
        await using var serviceProvider = await TransientFactoryServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var a = scope.GetRequiredService<IGreeter>();
        var b = scope.GetRequiredService<IGreeter>();

        await Assert.That(a).IsNotSameReferenceAs(b);
        await Assert.That(a.Greet()).IsNotEqualTo(b.Greet());
    }

    // === Test 4: Keyed factory methods ===

    [ServiceProvider]
    [Singleton<IGreeter, Greeter>(Key = "formal", FactoryMethod = nameof(CreateFormal))]
    [Singleton<IGreeter, Greeter>(Key = "casual", FactoryMethod = nameof(CreateCasual))]
    public partial class KeyedFactoryServiceProvider
    {
        public static Greeter CreateFormal() => new Greeter("Good day");
        public static Greeter CreateCasual() => new Greeter("Hey!");
    }

    [Test]
    public async Task KeyedFactoryMethod_UsesDifferentFactoriesPerKey()
    {
        await using var serviceProvider = await KeyedFactoryServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var formal = scope.GetRequiredService<IGreeter>("formal");
        var casual = scope.GetRequiredService<IGreeter>("casual");

        await Assert.That(formal.Greet()).IsEqualTo("Good day");
        await Assert.That(casual.Greet()).IsEqualTo("Hey!");
    }
}
