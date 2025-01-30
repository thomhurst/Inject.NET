using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class OpenGenericTestsWithTenant
{
    [Test]
    public async Task CanResolve_OpenGeneric_Directly()
    {
        var serviceProvider = await OpenGenericServiceProvider.BuildAsync();

        // Parent
        {
            await using var defaultScope = serviceProvider.CreateScope();

            var openGenericObject = defaultScope.GetRequiredService<IGeneric<IInterface>>();

            await Assert.That(openGenericObject).IsNotNull();

            var cast = await Assert.That(openGenericObject).IsTypeOf<Generic<IInterface>>();

            await Assert.That(cast?.Param).IsNotNull().And.IsTypeOf<Class1>();
        }
        
        // Tenant1
        {
            await using var tenant1Scope = serviceProvider.GetTenant<Tenant1>().CreateScope();

            var openGenericObject = tenant1Scope.GetRequiredService<IGeneric<IInterface>>();

            await Assert.That(openGenericObject).IsNotNull();

            var cast = await Assert.That(openGenericObject).IsTypeOf<Generic<IInterface>>();

            await Assert.That(cast?.Param).IsNotNull().And.IsTypeOf<TenantClass1>();
        }
        
        // Tenant2
        {
            await using var tenant2Scope = serviceProvider.GetTenant<Tenant2>().CreateScope();

            var openGenericObject = tenant2Scope.GetRequiredService<IGeneric<IInterface>>();

            await Assert.That(openGenericObject).IsNotNull();

            var cast = await Assert.That(openGenericObject).IsTypeOf<Generic<IInterface>>();

            await Assert.That(cast?.Param).IsNotNull().And.IsTypeOf<TenantClass2>();
        }
    }
    
    [Test]
    public async Task CanResolve_OpenGeneric_FromParent()
    {
        var serviceProvider = await OpenGenericServiceProvider.BuildAsync();

        // Parent
        {
            await using var defaultScope = serviceProvider.CreateScope();

            var parent = defaultScope.GetRequiredService<Parent>();
            var openGenericObject = parent.Generic;
            
            await Assert.That(openGenericObject).IsNotNull();

            var cast = await Assert.That(openGenericObject).IsTypeOf<Generic<IInterface>>();

            await Assert.That(cast?.Param).IsNotNull().And.IsTypeOf<Class1>();
        }
        
        // Tenant1
        {
            await using var tenant1Scope = serviceProvider.GetTenant<Tenant1>().CreateScope();

            var parent = tenant1Scope.GetRequiredService<Parent>();
            var openGenericObject = parent.Generic;
            
            await Assert.That(openGenericObject).IsNotNull();

            var cast = await Assert.That(openGenericObject).IsTypeOf<Generic<IInterface>>();

            await Assert.That(cast?.Param).IsNotNull().And.IsTypeOf<TenantClass1>();
        }
        
        // Tenant2
        {
            await using var tenant2Scope = serviceProvider.GetTenant<Tenant2>().CreateScope();

            var parent = tenant2Scope.GetRequiredService<Parent>();
            var openGenericObject = parent.Generic;

            await Assert.That(openGenericObject).IsNotNull();

            var cast = await Assert.That(openGenericObject).IsTypeOf<Generic<IInterface>>();

            await Assert.That(cast?.Param).IsNotNull().And.IsTypeOf<TenantClass2>();
        }
    }
    

    [ServiceProvider]
    [Transient<Parent>]
    [Transient<IInterface, Class1>]
    [Transient(typeof(IGeneric<>), typeof(Generic<>))]
    [WithTenant<Tenant1>]
    [WithTenant<Tenant2>]
    public partial class OpenGenericServiceProvider;
    
    public record Parent(IGeneric<IInterface> Generic);
    
    public interface IInterface;
    public interface IGeneric<T>;

    public record Class1 : IInterface;
    public record Generic<T>(T Param) : IGeneric<T>;

    [Transient<IInterface, TenantClass1>]
    public record Tenant1;
    
    [Transient<IInterface, TenantClass2>]
    public record Tenant2;
    
    public record TenantClass1 : IInterface;
    public record TenantClass2 : IInterface;

}