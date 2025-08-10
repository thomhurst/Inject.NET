// ReSharper disable All
#pragma warning disable CS9113 // Parameter is unread.

namespace Inject.NET.Attributes;

/// <summary>
/// Enables multi-tenant support for a service provider by specifying a tenant definition type.
/// This attribute allows the same service provider to serve different tenants with isolated service instances.
/// </summary>
/// <typeparam name="TTenantDefinition">The type that defines the tenant configuration and services</typeparam>
/// <example>
/// <code>
/// [ServiceProvider]
/// [WithTenant&lt;TenantA&gt;]
/// [WithTenant&lt;TenantB&gt;]
/// [Singleton&lt;ISharedService, SharedService&gt;]
/// public partial class MultiTenantServiceProvider;
/// 
/// public class TenantA 
/// {
///     // Tenant-specific configuration
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class WithTenantAttribute<TTenantDefinition> : Attribute where TTenantDefinition : class;