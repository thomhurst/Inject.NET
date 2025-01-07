// ReSharper disable All
#pragma warning disable CS9113 // Parameter is unread.

namespace Inject.NET.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class WithTenantAttribute<TTenantDefinition>(string tenantId) : Attribute where TTenantDefinition : class;