using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class OpenGenericTests
{
    [Test]
    public async Task CanResolve_OpenGeneric_Directly()
    {
        var serviceProvider = await OpenGenericServiceProvider.BuildAsync();

        await using var defaultScope = serviceProvider.CreateScope();

        var openGenericObject = defaultScope.GetRequiredService<IGeneric<Class1>>();

        await Assert.That(openGenericObject).IsNotNull();
        
        var cast = await Assert.That(openGenericObject).IsTypeOf<Generic<Class1>>();
        
        await Assert.That(cast?.Param).IsNotNull().And.IsTypeOf<Class1>();
    }
    
    [Test]
    public async Task CanResolve_OpenGeneric_FromParent()
    {
        var serviceProvider = await OpenGenericServiceProvider.BuildAsync();

        await using var defaultScope = serviceProvider.CreateScope();

        var parent = defaultScope.GetRequiredService<Parent>();

        await Assert.That(parent).IsNotNull();
        await Assert.That(parent).IsTypeOf<Parent>();

        await Assert.That(parent.Generic).IsNotNull();
        var cast = await Assert.That(parent.Generic).IsTypeOf<Generic<Class1>>();
        
        await Assert.That(cast?.Param).IsNotNull().And.IsTypeOf<Class1>();
    }
    

    [ServiceProvider]
    [Transient<Class1>]
    [Transient<Parent>]
    [Transient(typeof(IGeneric<>), typeof(Generic<>))]
    public partial class OpenGenericServiceProvider;
    
    public record Parent(IGeneric<Class1> Generic);
    
    public interface Interface1;
    public interface IGeneric<T>;

    public record Class1 : Interface1;
    public record Generic<T>(T Param) : IGeneric<T>;
}