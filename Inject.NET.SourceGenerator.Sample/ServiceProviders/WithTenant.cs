using Inject.NET.Attributes;
using Inject.NET.SourceGenerator.Sample.Models;

namespace Inject.NET.SourceGenerator.Sample.ServiceProviders;

[ServiceProvider]
[WithTenant<Tenant>]
[Singleton<Class1>]
public partial class WithTenant
{
    [Singleton<InheritsFromClass1>]
    public class Tenant;
}