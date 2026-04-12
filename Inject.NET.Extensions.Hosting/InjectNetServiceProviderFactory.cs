using Microsoft.Extensions.DependencyInjection;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Extensions.Hosting;

/// <summary>
/// An <see cref="IServiceProviderFactory{TContainerBuilder}"/> implementation that integrates
/// Inject.NET with ASP.NET Core / Generic Host. The factory receives a pre-built Inject.NET
/// provider and creates a child container layering the host's MEDI services on top.
/// </summary>
public sealed class InjectNetServiceProviderFactory : IServiceProviderFactory<InjectNetContainerBuilder>
{
    private readonly IServiceProvider _provider;

    /// <summary>
    /// Initializes a new instance with the pre-built Inject.NET provider.
    /// </summary>
    /// <param name="provider">The Inject.NET provider built before host setup.</param>
    public InjectNetServiceProviderFactory(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <inheritdoc />
    public InjectNetContainerBuilder CreateBuilder(IServiceCollection services)
    {
        return new InjectNetContainerBuilder(_provider, services);
    }

    /// <inheritdoc />
    public System.IServiceProvider CreateServiceProvider(InjectNetContainerBuilder containerBuilder)
    {
        return containerBuilder.Build();
    }
}
