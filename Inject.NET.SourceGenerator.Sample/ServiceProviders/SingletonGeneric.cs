using Inject.NET.Attributes;

namespace Inject.NET.SourceGenerator.Sample.ServiceProviders;

[ServiceProvider]
[Singleton(typeof(Generic<>))]
public partial class SingletonGeneric;