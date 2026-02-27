using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class FuncTests
{
    [Test]
    public async Task Func_Singleton_ReturnsSameInstanceEachCall()
    {
        await using var serviceProvider = await FuncServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var factory = scope.GetRequiredService<SingletonConsumer>();
        var a = factory.GetSingleton();
        var b = factory.GetSingleton();

        await Assert.That(a.Id).IsEqualTo(b.Id);
    }

    [Test]
    public async Task Func_Singleton_SameAcrossScopes()
    {
        await using var serviceProvider = await FuncServiceProvider.BuildAsync();
        await using var scope1 = serviceProvider.CreateScope();
        await using var scope2 = serviceProvider.CreateScope();

        var consumer1 = scope1.GetRequiredService<SingletonConsumer>();
        var consumer2 = scope2.GetRequiredService<SingletonConsumer>();

        var a = consumer1.GetSingleton();
        var b = consumer2.GetSingleton();

        await Assert.That(a.Id).IsEqualTo(b.Id);
    }

    [Test]
    public async Task Func_Transient_ReturnsNewInstanceEachCall()
    {
        await using var serviceProvider = await FuncServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var factory = scope.GetRequiredService<TransientConsumer>();
        var a = factory.GetTransient();
        var b = factory.GetTransient();

        await Assert.That(a.Id).IsNotEqualTo(b.Id);
    }

    [Test]
    public async Task Func_Scoped_ReturnsSameInstanceWithinScope()
    {
        await using var serviceProvider = await FuncServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var factory = scope.GetRequiredService<ScopedConsumer>();
        var a = factory.GetScoped();
        var b = factory.GetScoped();

        await Assert.That(a.Id).IsEqualTo(b.Id);
    }

    [Test]
    public async Task Func_Scoped_DifferentInstancesAcrossScopes()
    {
        await using var serviceProvider = await FuncServiceProvider.BuildAsync();
        await using var scope1 = serviceProvider.CreateScope();
        await using var scope2 = serviceProvider.CreateScope();

        var consumer1 = scope1.GetRequiredService<ScopedConsumer>();
        var consumer2 = scope2.GetRequiredService<ScopedConsumer>();

        var a = consumer1.GetScoped();
        var b = consumer2.GetScoped();

        await Assert.That(a.Id).IsNotEqualTo(b.Id);
    }

    [Test]
    public async Task Func_CanResolveViaGetService()
    {
        await using var serviceProvider = await FuncServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        // Test that Func<T> can be resolved via the runtime GetService(Type) path
        var funcType = typeof(Func<SingletonClass>);
        var result = scope.GetService(funcType);

        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsTypeOf<Func<SingletonClass>>();

        var factory = (Func<SingletonClass>)result!;
        var instance = factory();

        await Assert.That(instance).IsNotNull();
    }

    [Test]
    public async Task Func_Interface_ReturnsCorrectImplementation()
    {
        await using var serviceProvider = await FuncServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var consumer = scope.GetRequiredService<InterfaceConsumer>();
        var instance = consumer.GetService();

        await Assert.That(instance).IsTypeOf<ServiceImplementation>();
    }

    // Service types
    public class SingletonClass
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
    }

    public class TransientClass
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public class ScopedClass
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public interface IMyService
    {
        string Name { get; }
    }

    public class ServiceImplementation : IMyService
    {
        public string Name => "Implementation";
    }

    // Consumer types that use Func<T>
    public class SingletonConsumer(Func<SingletonClass> singletonFactory)
    {
        public SingletonClass GetSingleton() => singletonFactory();
    }

    public class TransientConsumer(Func<TransientClass> transientFactory)
    {
        public TransientClass GetTransient() => transientFactory();
    }

    public class ScopedConsumer(Func<ScopedClass> scopedFactory)
    {
        public ScopedClass GetScoped() => scopedFactory();
    }

    public class InterfaceConsumer(Func<IMyService> serviceFactory)
    {
        public IMyService GetService() => serviceFactory();
    }

    [ServiceProvider]
    [Singleton<SingletonClass>]
    [Transient<TransientClass>]
    [Scoped<ScopedClass>]
    [Singleton<IMyService, ServiceImplementation>]
    [Scoped<SingletonConsumer>]
    [Scoped<TransientConsumer>]
    [Scoped<ScopedConsumer>]
    [Scoped<InterfaceConsumer>]
    public partial class FuncServiceProvider;
}
