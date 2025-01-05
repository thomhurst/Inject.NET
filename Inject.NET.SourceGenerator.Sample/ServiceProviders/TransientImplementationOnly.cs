using Inject.NET.Attributes;

namespace Inject.NET.SourceGenerator.Sample.ServiceProviders;

[Transient<Class1>]
[ServiceProvider]
public partial class TransientImplementationOnly;