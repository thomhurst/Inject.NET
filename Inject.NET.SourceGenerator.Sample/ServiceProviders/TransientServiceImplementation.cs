using Inject.NET.Attributes;

namespace Inject.NET.SourceGenerator.Sample.ServiceProviders;

[Transient<Interface1, Class1>]
[ServiceProvider]
public partial class TransientServiceImplementation;