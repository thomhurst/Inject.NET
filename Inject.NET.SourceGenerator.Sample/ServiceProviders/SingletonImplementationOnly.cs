using Inject.NET.Attributes;

namespace Inject.NET.SourceGenerator.Sample.ServiceProviders;

[Singleton<Class1>]
[ServiceProvider]
public partial class SingletonImplementationOnly;