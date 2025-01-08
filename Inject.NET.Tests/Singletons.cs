using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class Singletons
{
    [Test]
    public async Task SameInstanceWhenResolvingMultipleTimes_FromSameScope()
    {
        await using var serviceProvider = await SingletonServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var a = scope.GetRequiredService<SingletonClass>();
        var b = scope.GetRequiredService<SingletonClass>();

        await Assert.That(a.Id).IsEqualTo(b.Id);
    }
    
    [Test]
    public async Task SameInstanceWhenResolvingMultipleTimes_FromDifferentScopes()
    {
        await using var serviceProvider = await SingletonServiceProvider.BuildAsync();

        var scope = serviceProvider.CreateScope();
        var scope2 = serviceProvider.CreateScope();

        var a = scope.GetRequiredService<SingletonClass>();
        var b = scope2.GetRequiredService<SingletonClass>();

        await scope.DisposeAsync();
        await scope2.DisposeAsync();
        
        await using var scope3 = serviceProvider.CreateScope();

        var c = scope3.GetRequiredService<SingletonClass>();
        
        await Assert.That(a.Id).IsEqualTo(b.Id).And.IsEqualTo(c.Id);
    }
    
    [Test]
    public async Task SameInstanceWhenResolvingMultipleTimes_IncludingTenantedScope()
    {
        await using var serviceProvider = await SingletonServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();
        await using var scope2 = serviceProvider.GetTenant("NonOverridingTenant").CreateScope();

        var a = scope.GetRequiredService<SingletonClass>();
        var b = scope2.GetRequiredService<SingletonClass>();
        
        await Assert.That(a.Id).IsEqualTo(b.Id);
    }
    
    [Test]
    public async Task Tenanted_DifferentInstanceWhenResolvingWithTenant_WithOverriddenType()
    {
        await using var serviceProvider = await SingletonServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();
        await using var scope2 = serviceProvider.GetTenant("OverridingTenant").CreateScope();

        var a = scope.GetRequiredService<SingletonClass>();
        var b = scope2.GetRequiredService<SingletonClass>();
        
        await Assert.That(a.Id).IsNotEqualTo(b.Id);
    }
    
    [Test]
    public async Task Tenanted_SameInstanceWhenResolvingMultipleTimes_FromSameScope()
    {
        await using var serviceProvider = await SingletonServiceProvider.BuildAsync();

        await using var scope = serviceProvider.GetTenant("OverridingTenant").CreateScope();

        var a = scope.GetRequiredService<SingletonClass>();
        var b = scope.GetRequiredService<SingletonClass>();

        await Assert.That(a.Id).IsEqualTo(b.Id);
    }
    
    [Test]
    public async Task Tenanted_SameInstanceWhenResolvingMultipleTimes_FromDifferentScopes()
    {
        await using var serviceProvider = await SingletonServiceProvider.BuildAsync();

        var scope = serviceProvider.GetTenant("OverridingTenant").CreateScope();
        var scope2 = serviceProvider.GetTenant("OverridingTenant").CreateScope();

        var a = scope.GetRequiredService<SingletonClass>();
        var b = scope2.GetRequiredService<SingletonClass>();

        await scope.DisposeAsync();
        await scope2.DisposeAsync();
        
        await using var scope3 = serviceProvider.GetTenant("OverridingTenant").CreateScope();

        var c = scope3.GetRequiredService<SingletonClass>();
        
        await Assert.That(a.Id).IsEqualTo(b.Id).And.IsEqualTo(c.Id);
    }
    
    public class SingletonClass
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    [ServiceProvider]
    [Singleton<SingletonClass>]
    [WithTenant<NonOverridingTenant>("NonOverridingTenant")]
    [WithTenant<OverridingTenant>("OverridingTenant")]
    public partial class SingletonServiceProvider
    {
        public record NonOverridingTenant;

        [Singleton<SingletonClass>]
        public record OverridingTenant;
    }
}