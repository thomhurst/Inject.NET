using Inject.NET.Attributes;

namespace Inject.NET.SourceGenerator.Sample.ServiceProviders;

[ServiceProvider]
[Singleton<Interface1, Class1>]
[Singleton<Interface2, Class2>]
[Singleton<Interface3, Class3>]
[Transient<Interface4, Class4>]
[Transient(typeof(IGeneric<>), typeof(Generic<>))]
[Transient<Wrapper>]

[Scoped<Interface5, Class5>]
public partial class AssortmentServiceProvider;

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

public class Wrapper(IGeneric<Class1> @interface) : IGeneric<Class1>;