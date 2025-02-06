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

    [Test]
    public async Task CanResolve_OpenGeneric_WithConstraints()
    {
        var serviceProvider = await OpenGenericServiceProvider.BuildAsync();

        await using var defaultScope = serviceProvider.CreateScope();

        var openGenericObject = defaultScope.GetRequiredService<IGenericWithConstraint<Class2>>();

        await Assert.That(openGenericObject).IsNotNull();

        var cast = await Assert.That(openGenericObject).IsTypeOf<GenericWithConstraint<Class2>>();

        await Assert.That(cast?.Param).IsNotNull().And.IsTypeOf<Class2>();
    }

    [Test]
    public async Task CanResolve_OpenGeneric_WithConstraints_FromParent()
    {
        var serviceProvider = await OpenGenericServiceProvider.BuildAsync();

        await using var defaultScope = serviceProvider.CreateScope();

        var parent = defaultScope.GetRequiredService<ParentWithConstraint>();

        await Assert.That(parent).IsNotNull();
        await Assert.That(parent).IsTypeOf<ParentWithConstraint>();

        await Assert.That(parent.Generic).IsNotNull();
        var cast = await Assert.That(parent.Generic).IsTypeOf<GenericWithConstraint<Class2>>();

        await Assert.That(cast?.Param).IsNotNull().And.IsTypeOf<Class2>();
    }

    [ServiceProvider]
    [Transient<Class1>]
    [Transient<Parent>]
    [Transient(typeof(IGeneric<>), typeof(Generic<>))]
    [Transient<Class2>]
    [Transient<ParentWithConstraint>]
    [Transient(typeof(IGenericWithConstraint<>), typeof(GenericWithConstraint<>))]
    public partial class OpenGenericServiceProvider;

    public record Parent(IGeneric<Class1> Generic);

    public record ParentWithConstraint(IGenericWithConstraint<Class2> Generic);

    public interface Interface1;
    public interface Interface2;
    public interface IGeneric<T>;
    public interface IGenericWithConstraint<T> where T : Interface2;

    public record Class1 : Interface1;
    public record Class2 : Interface2;

    public record Generic<T>(T Param) : IGeneric<T>;
    public record GenericWithConstraint<T>(T Param) : IGenericWithConstraint<T> where T : Interface2;
}