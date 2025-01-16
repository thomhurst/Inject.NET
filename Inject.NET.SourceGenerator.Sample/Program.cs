using Inject.NET.Attributes;
using Inject.NET.Extensions;

var serviceProvider = await MyServiceProvider.BuildAsync();

for (var i = 0; i < 1_000; i++)
{
    await using var scope = serviceProvider.CreateScope();
    
    scope.GetRequiredService<Interface5>();
}

[ServiceProvider]
[Scoped<Interface1, Class1>]
[Scoped<Interface2, Class2>]
[Scoped<Interface3, Class3>]
[Scoped<Interface4, Class4>]
[Scoped<Interface5, Class5>]
[WithTenant<Tenant>("Tenant")]
public partial class MyServiceProvider
{
    [Scoped<Interface1, Class1>]
    public record Tenant;
}

public interface Interface1;
public interface Interface2;
public interface Interface3;
public interface Interface4;
public interface Interface5;

public interface IGeneric<T>;

public class Class1 : Interface1;
public class Class2(Interface1 @interface) : Interface2;
public class Class3(Interface2 @interface) : Interface3;
public class Class4(Interface3 @interface) : Interface4;
public class Class5(Interface4 @interface) : Interface5;

public class Generic<T>(T t) : IGeneric<T>;