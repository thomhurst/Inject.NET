namespace Inject.NET.Models;

public record ServiceFactoryBuilders
{
    public List<ServiceDescriptor> Descriptors { get; } = [];

    public void Add(ServiceDescriptor descriptor)
    {
        Descriptors.Add(descriptor);
    }

    /// <summary>
    /// Checks whether a service descriptor for the specified service type (and optional key) already exists.
    /// </summary>
    /// <param name="serviceType">The service type to check for</param>
    /// <param name="key">The optional service key</param>
    /// <returns>True if a descriptor for the service type already exists; otherwise false</returns>
    public bool HasService(Type serviceType, string? key = null)
    {
        foreach (var descriptor in Descriptors)
        {
            if (descriptor.ServiceType == serviceType && descriptor.Key == key)
            {
                return true;
            }
        }

        return false;
    }
}