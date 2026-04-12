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
    private ChildServiceProvider? _built;

    internal InjectNetContainerBuilder(IServiceProvider provider, IServiceCollection services)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Builds a child container that merges the host's MEDI service registrations
    /// into the pre-built Inject.NET provider. This is a synchronous operation.
    /// May only be called once per builder instance.
    /// </summary>
    /// <returns>A <see cref="ChildServiceProvider"/> that can resolve both compile-time
    /// Inject.NET services and runtime MEDI services.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="Build"/> is called more than once on the same builder,
    /// or when replaying the host's <see cref="IServiceCollection"/> into the Inject.NET
    /// registrar fails. The original exception is attached as the inner exception.
    /// </exception>
    public ChildServiceProvider Build()
    {
        if (_built is not null)
        {
            throw new InvalidOperationException(
                $"{nameof(InjectNetContainerBuilder)}.{nameof(Build)} may only be called once. " +
                "Each host requires its own builder instance.");
        }

        try
        {
            _built = _provider.CreateChildContainer(registrar =>
            {
                registrar.AddServiceCollection(_services);
            });
        }
        catch (Exception ex) when (ex is not InvalidOperationException
                                      && ex is not NotSupportedException
                                      && ex is not ArgumentException)
        {
            throw new InvalidOperationException(
                "Failed to replay the host's service collection into the Inject.NET child container. " +
                "See the inner exception for the underlying registration error.",
                ex);
        }

        return _built;
    }
}
