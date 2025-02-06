using Inject.NET.Attributes;

namespace Inject.NET.Tests;

public partial class CircularDependencyTests
{
    [Test]
    public async Task Test()
    {
        var provider = await CircularDependencyServiceProvider.BuildAsync();
        
        await using var scope = provider.CreateTypedScope();
    }

    public class Class1(Class2 @class);
    public class Class2(Class1 @class);

    [ServiceProvider]
    [Scoped<Class1>]
    [Scoped<Class2>]
    public partial class CircularDependencyServiceProvider;
}