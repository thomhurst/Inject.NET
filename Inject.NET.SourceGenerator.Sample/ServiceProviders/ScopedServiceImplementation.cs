using Inject.NET.Attributes;

namespace Inject.NET.SourceGenerator.Sample.ServiceProviders;

[Scoped<Interface1, Class1>]
[ServiceProvider]
public partial class ScopedServiceImplementation;