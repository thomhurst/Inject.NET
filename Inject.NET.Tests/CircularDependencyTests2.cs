using Inject.NET.Attributes;

namespace Inject.NET.Tests;

public partial class CircularDependencyTests2
{
    [Test]
    public async Task Test()
    {
        var provider = await CircularDependencyServiceProvider.BuildAsync();
        
        await using var scope = provider.CreateTypedScope();
    }

    public interface Interface1;
    public interface Interface2;
    public class Class1(Interface2 @class) : Interface1;
    public class Class2(Interface1 @class) : Interface2;

    [ServiceProvider]
    [Scoped<Interface1, Class1>]
    [Scoped<Interface2, Class2>]
    public partial class CircularDependencyServiceProvider;
}