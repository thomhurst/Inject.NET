using Inject.NET.Attributes;
using Inject.NET.SourceGenerator.Sample.Models;

namespace Inject.NET.SourceGenerator.Sample.ServiceProviders;

[ServiceProvider]
[Singleton(typeof(IGeneric<>), typeof(Generic<>))]
public partial class SingletonGeneric2;