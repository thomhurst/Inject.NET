using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Microsoft.Testing.Platform.Services;

namespace Inject.NET.Tests;

public partial class OpenGenericTests
{
    [Test]
    public async Task CanResolve_OpenGeneric()
    {
        var serviceProvider = await OpenGenericServiceProvider.BuildAsync();

        await using var defaultScope = serviceProvider.CreateScope();

        var openGenericObject = defaultScope.GetRequiredService<IGeneric<Class1>>();

        await Assert.That(openGenericObject).IsNotNull();
        
        var cast = await Assert.That(openGenericObject).IsTypeOf<Generic<Class1>>();
        
        await Assert.That(cast?.Param).IsNotNull().And.IsTypeOf<Class1>();
    }
    

    [ServiceProvider]
    [Transient<Class1>]
    [Transient(typeof(IGeneric<>), typeof(Generic<>))]
    public partial class OpenGenericServiceProvider;
    
    public interface Interface1;
    public interface IGeneric<T>;

    public class Class1 : Interface1;
    public record Generic<T>(T Param) : IGeneric<T>;
}