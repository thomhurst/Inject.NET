namespace Inject.NET.Models;

public record ServiceFactoryBuilders
{
    public List<ServiceDescriptor> Descriptors { get; } = [];

    public void Add(ServiceDescriptor descriptor)
    {
        Descriptors.Add(descriptor);
    }
}