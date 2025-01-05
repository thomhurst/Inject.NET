#pragma warning disable CS9113 // Parameter is unread.
namespace Inject.NET.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ServiceKeyAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}