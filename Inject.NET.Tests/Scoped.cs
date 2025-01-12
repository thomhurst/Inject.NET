using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class Scoped
{
    [Test]
    public async Task SameInstanceWhenResolvingMultipleTimes_FromSameScope()
    {
        await using var serviceProvider = await ScopedServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var a = scope.GetRequiredService<ScopedClass>();
        var b = scope.GetRequiredService<ScopedClass>();

        await Assert.That(a.Id).IsEqualTo(b.Id);
    }
    
    [Test]
    public async Task SameInstanceWhenResolvingMultipleTimes_FromDifferentScopes()
    {
        await using var serviceProvider = await ScopedServiceProvider.BuildAsync();

        var scope = serviceProvider.CreateScope();
        var scope2 = serviceProvider.CreateScope();

        var a = scope.GetRequiredService<ScopedClass>();
        var b = scope2.GetRequiredService<ScopedClass>();

        await scope.DisposeAsync();
        await scope2.DisposeAsync();
        
        await using var scope3 = serviceProvider.CreateScope();

        var c = scope3.GetRequiredService<ScopedClass>();
        
        await Assert.That(a.Id).IsEqualTo(b.Id).And.IsEqualTo(c.Id);
    }
    
    [Test]
    public async Task SameInstanceWhenResolvingMultipleTimes_IncludingTenantedScope()
    {
        await using var serviceProvider = await ScopedServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();
        await using var scope2 = serviceProvider.GetTenant("NonOverridingTenant").CreateScope();

        var a = scope.GetRequiredService<ScopedClass>();
        var b = scope2.GetRequiredService<ScopedClass>();
        
        await Assert.That(a.Id).IsEqualTo(b.Id);
    }
    
    [Test]
    public async Task Tenanted_DifferentInstanceWhenResolvingWithTenant_WithOverriddenType()
    {
        await using var serviceProvider = await ScopedServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();
        await using var scope2 = serviceProvider.GetTenant("OverridingTenant").CreateScope();

        var a = scope.GetRequiredService<ScopedClass>();
        var b = scope2.GetRequiredService<ScopedClass>();
        
        await Assert.That(a.Id).IsNotEqualTo(b.Id);
    }
    
    [Test]
    public async Task Tenanted_SameInstanceWhenResolvingMultipleTimes_FromSameScope()
    {
        await using var serviceProvider = await ScopedServiceProvider.BuildAsync();

        await using var scope = serviceProvider.GetTenant("OverridingTenant").CreateScope();

        var a = scope.GetRequiredService<ScopedClass>();
        var b = scope.GetRequiredService<ScopedClass>();

        await Assert.That(a.Id).IsEqualTo(b.Id);
    }
    
    [Test]
    public async Task Tenanted_SameInstanceWhenResolvingMultipleTimes_FromDifferentScopes()
    {
        await using var serviceProvider = await ScopedServiceProvider.BuildAsync();

        var scope = serviceProvider.GetTenant("OverridingTenant").CreateScope();
        var scope2 = serviceProvider.GetTenant("OverridingTenant").CreateScope();

        var a = scope.GetRequiredService<ScopedClass>();
        var b = scope2.GetRequiredService<ScopedClass>();

        await scope.DisposeAsync();
        await scope2.DisposeAsync();
        
        await using var scope3 = serviceProvider.GetTenant("OverridingTenant").CreateScope();

        var c = scope3.GetRequiredService<ScopedClass>();
        
        await Assert.That(a.Id).IsEqualTo(b.Id).And.IsEqualTo(c.Id);
    }
    
    public class ScopedClass
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    [ServiceProvider]
    [Scoped<ScopedClass>]
    [WithTenant<NonOverridingTenant>("NonOverridingTenant")]
    [WithTenant<OverridingTenant>("OverridingTenant")]
    public partial class ScopedServiceProvider
    {
        public record NonOverridingTenant;

        [Scoped<ScopedClass>]
        public record OverridingTenant;
    }
}