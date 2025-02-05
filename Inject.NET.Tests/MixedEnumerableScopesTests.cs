using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class MixedEnumerableScopesTests
{
    [Test]
    public async Task DifferentInstanceWhenResolvingMultipleTimes_FromSameScope()
    {
        await using var serviceProvider = await MixedEnumerableScopesServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        var services = scope.GetServices<MyClass>();

        await using var nonOverridingTenantScope = serviceProvider.GetTenant<MixedEnumerableScopesServiceProvider.NonOverridingTenant>().CreateScope();
        await using var overridingTenantScope = serviceProvider.GetTenant<MixedEnumerableScopesServiceProvider.OverridingTenant>().CreateScope();

        var nonOverridingTenantServices = nonOverridingTenantScope.GetServices<MyClass>();
        var overridingTenantServices = overridingTenantScope.GetServices<MyClass>();
        
        await Assert.That(services).HasCount().EqualTo(9);
        await Assert.That(nonOverridingTenantServices).HasCount().EqualTo(9);
        await Assert.That(overridingTenantServices).HasCount().EqualTo(12);
    }
    
    public class MyClass
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    [ServiceProvider]
    [Transient<MyClass>]
    [Scoped<MyClass>]
    [Singleton<MyClass>]
    [Scoped<MyClass>]
    [Singleton<MyClass>]
    [Transient<MyClass>]
    [Singleton<MyClass>]
    [Transient<MyClass>]
    [Scoped<MyClass>]
    [WithTenant<NonOverridingTenant>]
    [WithTenant<OverridingTenant>]
    public partial class MixedEnumerableScopesServiceProvider
    {
        public record NonOverridingTenant;

        [Singleton<MyClass>]
        [Transient<MyClass>]
        [Scoped<MyClass>]
        public record OverridingTenant;
    }
}