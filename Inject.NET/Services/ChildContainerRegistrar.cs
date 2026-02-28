using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

/// <summary>
/// A simple service registrar used to collect service descriptors for a child container.
/// This registrar does not build a full provider; instead, the collected descriptors
/// are merged with a parent provider's registrations.
/// </summary>
public sealed class ChildContainerRegistrar : IServiceRegistrar
{
    /// <inheritdoc />
    public ServiceFactoryBuilders ServiceFactoryBuilders { get; } = new();

    /// <inheritdoc />
    public IServiceRegistrar Register(ServiceDescriptor descriptor)
    {
        ServiceFactoryBuilders.Add(descriptor);
        return this;
    }
}
