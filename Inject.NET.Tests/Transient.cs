using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class Transient
{
    [Test]
    public async Task DifferentInstanceWhenResolvingMultipleTimes_FromSameScope()
    {
        await using var serviceProvider = await TransientServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var a = scope.GetRequiredService<TransientClass>();
        var b = scope.GetRequiredService<TransientClass>();

        await Assert.That(a.Id).IsNotEqualTo(b.Id);
    }
    
    [Test]
    public async Task DifferentInstanceWhenResolvingMultipleTimes_FromDifferentScopes()
    {
        await using var serviceProvider = await TransientServiceProvider.BuildAsync();

        var scope = serviceProvider.CreateScope();
        var scope2 = serviceProvider.CreateScope();

        var a = scope.GetRequiredService<TransientClass>();
        var b = scope2.GetRequiredService<TransientClass>();

        await scope.DisposeAsync();
        await scope2.DisposeAsync();
        
        await using var scope3 = serviceProvider.CreateScope();

        var c = scope3.GetRequiredService<TransientClass>();
        
        await Assert.That(a.Id).IsNotEqualTo(b.Id).And.IsNotEqualTo(c.Id);
        await Assert.That(b.Id).IsNotEqualTo(c.Id);
    }
    
    [Test]
    public async Task DifferentInstanceWhenResolvingMultipleTimes_IncludingTenantedScope()
    {
        await using var serviceProvider = await TransientServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();
        await using var scope2 = serviceProvider.GetTenant<TransientServiceProvider.NonOverridingTenant>().CreateScope();

        var a = scope.GetRequiredService<TransientClass>();
        var b = scope2.GetRequiredService<TransientClass>();
        
        await Assert.That(a.Id).IsNotEqualTo(b.Id);
    }
    
    [Test]
    public async Task Tenanted_DifferentInstanceWhenResolvingWithTenant_WithOverriddenType()
    {
        await using var serviceProvider = await TransientServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();
        await using var scope2 = serviceProvider.GetTenant<TransientServiceProvider.OverridingTenant>().CreateScope();

        var a = scope.GetRequiredService<TransientClass>();
        var b = scope2.GetRequiredService<TransientClass>();
        
        await Assert.That(a.Id).IsNotEqualTo(b.Id);
    }
    
    [Test]
    public async Task Tenanted_DifferentInstanceWhenResolvingMultipleTimes_FromSameScope()
    {
        await using var serviceProvider = await TransientServiceProvider.BuildAsync();

        await using var scope = serviceProvider.GetTenant<TransientServiceProvider.OverridingTenant>().CreateScope();

        var a = scope.GetRequiredService<TransientClass>();
        var b = scope.GetRequiredService<TransientClass>();

        await Assert.That(a.Id).IsNotEqualTo(b.Id);
    }
    
    [Test]
    public async Task Tenanted_DifferentInstanceWhenResolvingMultipleTimes_FromDifferentScopes()
    {
        await using var serviceProvider = await TransientServiceProvider.BuildAsync();

        var scope = serviceProvider.GetTenant<TransientServiceProvider.OverridingTenant>().CreateScope();
        var scope2 = serviceProvider.GetTenant<TransientServiceProvider.OverridingTenant>().CreateScope();

        var a = scope.GetRequiredService<TransientClass>();
        var b = scope2.GetRequiredService<TransientClass>();

        await scope.DisposeAsync();
        await scope2.DisposeAsync();
        
        await using var scope3 = serviceProvider.GetTenant<TransientServiceProvider.OverridingTenant>().CreateScope();

        var c = scope3.GetRequiredService<TransientClass>();
        
        await Assert.That(a.Id).IsNotEqualTo(b.Id).And.IsNotEqualTo(c.Id);
        await Assert.That(b.Id).IsNotEqualTo(c.Id);
    }
    
    public class TransientClass
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public class ClassContainingTransient(TransientClass transientClass);

    [ServiceProvider]
    [Transient<TransientClass>]
    [Transient<ClassContainingTransient>]
    [WithTenant<NonOverridingTenant>]
    [WithTenant<OverridingTenant>]
    public partial class TransientServiceProvider
    {
        public record NonOverridingTenant;

        [Transient<TransientClass>]
        public record OverridingTenant;
    }
}