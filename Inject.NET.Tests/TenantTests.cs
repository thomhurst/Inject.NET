using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class TenantTests
{
    [Test]
    public async Task CanOverrideTenantObjects_ResolvingObjectsDirectly()
    {
        var serviceProvider = await ServiceProvider.BuildAsync();

        await using var defaultScope = serviceProvider.CreateScope();
        await using var tenant1Scope = serviceProvider.GetTenant<Tenant1>().CreateScope();
        await using var tenant2Scope = serviceProvider.GetTenant<Tenant2>().CreateScope();

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

        //await using var defaultScope = serviceProvider.CreateScope();
        await using var tenant1Scope = serviceProvider.GetTenant<Tenant1>().CreateScope();
        await using var tenant2Scope = serviceProvider.GetTenant<Tenant2>().CreateScope();

        // await Assert.That(defaultScope.GetRequiredService<Parent>().Get())
        //     .IsEqualTo("DefaultChild");

        await Assert.That(tenant1Scope.GetRequiredService<Parent>().Get())
            .IsEqualTo("Tenant1Child");
        
        await Assert.That(tenant2Scope.GetRequiredService<Parent>().Get())
            .IsEqualTo("Tenant2Child");
    }
    
    [Test]
    public async Task CanOverrideTenantObjects_ResolvingViaParent_Typed()
    {
        var serviceProvider = await ServiceProvider.BuildAsync();

        //await using var defaultScope = serviceProvider.CreateScope();
        await using var tenant1Scope = serviceProvider.Tenant_Tenant1.CreateTypedScope();
        await using var tenant2Scope = serviceProvider.Tenant_Tenant2.CreateTypedScope();

        // await Assert.That(defaultScope.GetRequiredService<Parent>().Get())
        //     .IsEqualTo("DefaultChild");

        await Assert.That(tenant1Scope.Inject__NET__Tests__TenantTests__Parent____0.Get())
            .IsEqualTo("Tenant1Child");
        
        await Assert.That(tenant2Scope.Inject__NET__Tests__TenantTests__IChild____0.Get())
            .IsEqualTo("Tenant2Child");
    }

    [Scoped<Parent>]
    [Scoped<IChild, DefaultChild>]
    [WithTenant<Tenant1>]
    [WithTenant<Tenant2>]
    [ServiceProvider]
    public partial class ServiceProvider;

    [Scoped<IChild, Tenant1Child>]
    public class Tenant1;
    
    [Scoped<IChild, Tenant2Child>]
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