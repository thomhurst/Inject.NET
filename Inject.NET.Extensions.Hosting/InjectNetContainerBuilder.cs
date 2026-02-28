using Inject.NET.Extensions.DependencyInjection;
using Inject.NET.Services;
using Microsoft.Extensions.DependencyInjection;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Extensions.Hosting;

/// <summary>
/// Container builder that holds a pre-built Inject.NET provider and the host's
/// <see cref="IServiceCollection"/>. When <see cref="Build"/> is called, it creates
/// a synchronous child container that layers the host's MEDI services on top.
/// </summary>
public sealed class InjectNetContainerBuilder
{
    private readonly IServiceProvider _provider;
    private readonly IServiceCollection _services;

    internal InjectNetContainerBuilder(IServiceProvider provider, IServiceCollection services)
    {
        _provider = provider;
        _services = services;
    }

    /// <summary>
    /// Builds a child container that merges the host's MEDI service registrations
    /// into the pre-built Inject.NET provider. This is a synchronous operation.
    /// </summary>
    /// <returns>A <see cref="ChildServiceProvider"/> that can resolve both compile-time
    /// Inject.NET services and runtime MEDI services.</returns>
    public ChildServiceProvider Build()
    {
        return _provider.CreateChildContainer(registrar =>
        {
            registrar.AddServiceCollection(_services);
        });
    }
}
