using Inject.NET.Attributes;

namespace Inject.NET.SourceGenerator.Sample.ServiceProviders;

[ServiceProvider]
[WithTenant<Tenant>("tenant1")]
[Transient<Parent>]
[Transient<IChild, Child1>]
public partial class WithTenantOverridingType
{
    [Transient<IChild, TenantChild1>]
    public class Tenant;

    public class Parent(IChild child);

    public interface IChild;

    public class Child1 : IChild;
    public class TenantChild1 : IChild;
}