using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Microsoft.Testing.Platform.Services;

namespace Inject.NET.Tests;

public partial class TenantTests
{
    [Test]
    public async Task CanOverrideTenantObjects_ResolvingObjectsDirectly()
    {
        var serviceProvider = await ServiceProvider.BuildAsync();

        await using var defaultScope = serviceProvider.CreateScope();
        await using var tenant1Scope = serviceProvider.GetTenant("tenant1").CreateScope();
        await using var tenant2Scope = serviceProvider.GetTenant("tenant2").CreateScope();

        await Assert.That(defaultScope.GetRequiredService<IChild>().Get())
            .IsEqualTo("DefaultChild");

        await Assert.That(tenant1Scope.GetRequiredService<IChild>().Get())
            .IsEqualTo("Tenant1Child");
        
        await Assert.That(tenant2Scope.GetRequiredService<IChild>().Get())
            .IsEqualTo("Tenant2Child");
    }
    
    [Test]
    public async Task CanOverrideTenantObjects_ResolvingViaParent()
    {
        var serviceProvider = await ServiceProvider.BuildAsync();

        await using var defaultScope = serviceProvider.CreateScope();
        await using var tenant1Scope = serviceProvider.GetTenant("tenant1").CreateScope();
        await using var tenant2Scope = serviceProvider.GetTenant("tenant2").CreateScope();

        await Assert.That(defaultScope.GetRequiredService<Parent>().Get())
            .IsEqualTo("DefaultChild");

        await Assert.That(tenant1Scope.GetRequiredService<Parent>().Get())
            .IsEqualTo("Tenant1Child");
        
        await Assert.That(tenant2Scope.GetRequiredService<Parent>().Get())
            .IsEqualTo("Tenant2Child");
    }

    [Transient<Parent>]
    [Transient<IChild, DefaultChild>]
    [WithTenant<Tenant1>("tenant1")]
    [WithTenant<Tenant2>("tenant2")]
    [ServiceProvider]
    public partial class ServiceProvider;

    [Transient<IChild, Tenant1Child>]
    public class Tenant1;
    
    [Transient<IChild, Tenant2Child>]
    public class Tenant2;
    
    public class Parent(IChild child)
    {
        public string Get() => child.Get();
    }
    
    public interface IChild
    {
        string Get();
    }
    
    public class DefaultChild : IChild
    {
        public string Get()
        {
            return GetType().Name;
        }
    }
    
    public class Tenant1Child : IChild
    {
        public string Get()
        {
            return GetType().Name;
        }
    }
    
    public class Tenant2Child : IChild
    {
        public string Get()
        {
            return GetType().Name;
        }
    }
}