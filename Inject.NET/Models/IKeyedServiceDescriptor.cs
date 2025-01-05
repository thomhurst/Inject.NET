namespace Inject.NET.Models;

public interface IKeyedServiceDescriptor : IServiceDescriptor
{
    string Key { get; init; }
}