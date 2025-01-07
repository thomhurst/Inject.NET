using Inject.NET.Attributes;

namespace Inject.NET.SourceGenerator.Sample.ServiceProviders;

[ServiceProvider]
[Transient<Class1>]
[Transient(typeof(IGeneric<>), typeof(Generic<>))]
[Transient<Wrapper>]
public partial class OpenGeneric5
{
    public interface Interface1;

    public interface IGeneric<T>;

    public class Class1 : Interface1;

    public class Generic<T>(T t) : IGeneric<T>;
    public class Wrapper(IGeneric<Class1> obj);
}