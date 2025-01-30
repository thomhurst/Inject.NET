using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class AITests
{
    [Test]
    public async Task TestSingletonService()
    {
        var serviceProvider = await MyServiceProvider.BuildAsync();
        
        var singleton1 = serviceProvider.CreateScope().GetRequiredService<SingletonService>();
        var singleton2 = serviceProvider.CreateScope().GetRequiredService<SingletonService>();
        
        await Assert.That(singleton1).IsSameReferenceAs(singleton2);
    }

    [Test]
    public async Task TestScopedService()
    {
        var serviceProvider = await MyServiceProvider.BuildAsync();
        
        await using var scope1 = serviceProvider.CreateScope();
        await using var scope2 = serviceProvider.CreateScope();
        
        var scoped1 = scope1.GetRequiredService<ScopedService>();
        var scoped2 = scope1.GetRequiredService<ScopedService>();
        var scoped3 = scope2.GetRequiredService<ScopedService>();
        
        await Assert.That(scoped1).IsSameReferenceAs(scoped2);
        await Assert.That(scoped1).IsNotSameReferenceAs(scoped3);
    }

    [Test]
    public async Task TestTransientService()
    {
        var serviceProvider = await MyServiceProvider.BuildAsync();
        
        var transient1 = serviceProvider.CreateScope().GetRequiredService<TransientService>();
        var transient2 = serviceProvider.CreateScope().GetRequiredService<TransientService>();
        
        await Assert.That(transient1).IsNotSameReferenceAs(transient2);
    }

    [Test]
    public async Task TestGenericService()
    {
        var serviceProvider = await MyServiceProvider.BuildAsync();
        
        var generic1 = serviceProvider.CreateScope().GetRequiredService<IGeneric<SingletonService>>();
        var generic2 = serviceProvider.CreateScope().GetRequiredService<IGeneric<SingletonService>>();
        
        await Assert.That(generic1).IsNotNull();
        await Assert.That(generic1).IsNotSameReferenceAs(generic2);
    }

    [Test]
    public async Task TestNestedService()
    {
        var serviceProvider = await MyServiceProvider.BuildAsync();
        
        var nested = serviceProvider.CreateScope().GetRequiredService<NestedService>();
        
        await Assert.That(nested).IsNotNull();
        await Assert.That(nested.SingletonService).IsNotNull();
        await Assert.That(nested.ScopedService).IsNotNull();
        await Assert.That(nested.TransientService).IsNotNull();
    }

    [ServiceProvider]
    [Singleton<SingletonService>]
    [Scoped<ScopedService>]
    [Transient<TransientService>]
    [Transient(typeof(IGeneric<>), typeof(Generic<>))]
    [Transient<NestedService>]
    public partial class MyServiceProvider;

    public class SingletonService { }

    public class ScopedService { }

    public class TransientService { }

    public interface IGeneric<T> { }

    public class Generic<T> : IGeneric<T>
    {
        public Generic(T service) { }
    }

    public class NestedService
    {
        public SingletonService SingletonService { get; }
        public ScopedService ScopedService { get; }
        public TransientService TransientService { get; }

        public NestedService(SingletonService singletonService, ScopedService scopedService, TransientService transientService)
        {
            SingletonService = singletonService;
            ScopedService = scopedService;
            TransientService = transientService;
        }
    }
}