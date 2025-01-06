namespace Inject.NET.Models;

public record ServiceFactoryBuilders
{
    public List<IServiceDescriptor> Descriptors { get; } = [];

    public void Add(ServiceDescriptor descriptor)
    {
        Descriptors.Add(descriptor);
    }
}