using Inject.NET.Attributes;
using Inject.NET.SourceGenerator.Sample.Models;

namespace Inject.NET.SourceGenerator.Sample.ServiceProviders;

[Scoped<IClass, Class1>]
[ServiceProvider]
public partial class ScopedServiceImplementation;