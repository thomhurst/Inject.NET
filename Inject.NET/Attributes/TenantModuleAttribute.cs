namespace Inject.NET.Attributes;

/// <summary>
/// Marks a class as a tenant module that defines tenant-specific service overrides.
/// Classes marked with this attribute contain registration attributes (e.g., [Singleton], [Scoped], [Transient])
/// that override or extend the root service provider's registrations for a specific tenant.
/// Use with <see cref="WithTenantAttribute{TTenantDefinition}"/> on the service provider.
/// </summary>
/// <example>
/// <code>
/// [TenantModule]
/// [Scoped&lt;IRepository, PremiumRepository&gt;]
/// public class PremiumTenant;
///
/// [ServiceProvider]
/// [Scoped&lt;IRepository, DefaultRepository&gt;]
/// [WithTenant&lt;PremiumTenant&gt;]
/// public partial class MyServiceProvider;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class)]
public sealed class TenantModuleAttribute : Attribute;
