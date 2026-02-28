using Microsoft.Extensions.Hosting;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Extensions.Hosting;

/// <summary>
/// Extension methods for integrating Inject.NET with the .NET Generic Host.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures the host to use an existing Inject.NET service provider as the
    /// backing DI container. The host's MEDI services are layered on top via a child container.
    /// </summary>
    /// <param name="hostBuilder">The host builder to configure.</param>
    /// <param name="provider">The pre-built Inject.NET service provider.</param>
    /// <returns>The host builder for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// await using var provider = await MyServiceProvider.BuildAsync();
    /// var host = Host.CreateDefaultBuilder(args)
    ///     .UseInjectNet(provider)
    ///     .Build();
    /// </code>
    /// </example>
    public static IHostBuilder UseInjectNet(this IHostBuilder hostBuilder, IServiceProvider provider)
    {
        return hostBuilder.UseServiceProviderFactory(new InjectNetServiceProviderFactory(provider));
    }
}
