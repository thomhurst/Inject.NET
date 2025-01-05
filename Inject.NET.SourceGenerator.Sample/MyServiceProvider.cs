using Inject.NET.Attributes;
using Inject.NET.SourceGenerator.Sample.Models;

namespace Inject.NET.SourceGenerator.Sample;

[Singleton<Class1>]
[Singleton<Class2>]
[Singleton<Class3>]
[Singleton<Class4>]
[Scoped<Class5>]
[Scoped<Class6>]
[Singleton<IClass, Class1>]
[ServiceProvider]
public partial class MyServiceProvider;