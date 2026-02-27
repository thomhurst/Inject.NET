using Inject.NET.Interfaces;

namespace Inject.NET.Extensions;

/// <summary>
/// Extension methods for assembly scanning-based service registration.
/// Provides convention-based registration by scanning assemblies for service implementations.
/// </summary>
public static class ServiceRegistrarScanExtensions
{
    /// <summary>
    /// Scans assemblies for service implementations and registers them with the container
    /// based on the configured conventions.
    /// </summary>
    /// <param name="registrar">The service registrar</param>
    /// <param name="configure">An action that configures the assembly scanner</param>
    /// <returns>The registrar for fluent chaining</returns>
    /// <example>
    /// <code>
    /// partial void ConfigureServices()
    /// {
    ///     this.Scan(scanner =>
    ///     {
    ///         scanner.FromAssemblyOf&lt;MyService&gt;();
    ///         scanner.AddAllTypesOf&lt;ICommandHandler&gt;();
    ///         scanner.WithDefaultConventions();
    ///         scanner.AsSingleton();
    ///     });
    /// }
    /// </code>
    /// </example>
    public static IServiceRegistrar Scan(
        this IServiceRegistrar registrar,
        Action<AssemblyScanner> configure)
    {
        var scanner = new AssemblyScanner();
        configure(scanner);
        scanner.Apply(registrar);
        return registrar;
    }
}
