using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.SourceGenerator.Sample.ServiceProviders;

var serviceProvider = await MyServiceProvider.BuildAsync();

for (var i = 0; i < 1_000; i++)
{
    await using var scope = serviceProvider.CreateScope();
    
    scope.GetRequiredService<Interface5>();
}

[ServiceProvider]
[Singleton<Interface1, Class1>]
[Singleton<Interface2, Class2>]
[Singleton<Interface3, Class3>]
[Singleton<Interface4, Class4>]
[Singleton<Interface5, Class5>]
[WithTenant<Tenant>("Tenant")]
public partial class MyServiceProvider
{
    [Singleton<Interface1, Class1>]
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