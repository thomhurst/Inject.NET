using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class ModuleTests
{
    [Test]
    public async Task Module_ServicesResolveCorrectly()
    {
        await using var serviceProvider = await ModuleServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var logger = scope.GetRequiredService<IModuleLogger>();
        var repo = scope.GetRequiredService<IModuleRepo>();

        await Assert.That(logger).IsNotNull();
        await Assert.That(repo).IsNotNull();
    }

    [Test]
    public async Task Module_SingletonLifetimePreserved()
    {
        await using var serviceProvider = await ModuleServiceProvider.BuildAsync();

        await using var scope1 = serviceProvider.CreateScope();
        await using var scope2 = serviceProvider.CreateScope();

        var a = scope1.GetRequiredService<IModuleLogger>();
        var b = scope2.GetRequiredService<IModuleLogger>();

        await Assert.That(a).IsSameReferenceAs(b);
    }

    [Test]
    public async Task Module_ScopedLifetimePreserved()
    {
        await using var serviceProvider = await ModuleServiceProvider.BuildAsync();

        await using var scope1 = serviceProvider.CreateScope();
        await using var scope2 = serviceProvider.CreateScope();

        var a = scope1.GetRequiredService<IModuleRepo>();
        var sameScope = scope1.GetRequiredService<IModuleRepo>();
        var b = scope2.GetRequiredService<IModuleRepo>();

        await Assert.That(a).IsSameReferenceAs(sameScope);
        await Assert.That(a).IsNotSameReferenceAs(b);
    }

    [Test]
    public async Task Module_DirectRegistrationsCoexistWithModuleRegistrations()
    {
        await using var serviceProvider = await ModuleServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var logger = scope.GetRequiredService<IModuleLogger>();
        var handler = scope.GetRequiredService<ModuleHandler>();

        await Assert.That(logger).IsNotNull();
        await Assert.That(handler).IsNotNull();
    }

    [Test]
    public async Task Module_DependenciesAcrossModulesResolve()
    {
        await using var serviceProvider = await CrossModuleDepsProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var consumer = scope.GetRequiredService<CrossModuleConsumer>();

        await Assert.That(consumer).IsNotNull();
        await Assert.That(consumer.Logger).IsNotNull();
    }

    [Test]
    public async Task NestedModules_ServicesFromNestedModuleResolve()
    {
        await using var serviceProvider = await NestedModuleProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var logger = scope.GetRequiredService<IModuleLogger>();
        var repo = scope.GetRequiredService<IModuleRepo>();
        var handler = scope.GetRequiredService<NestedHandler>();

        await Assert.That(logger).IsNotNull();
        await Assert.That(repo).IsNotNull();
        await Assert.That(handler).IsNotNull();
    }

    // --- Service types ---

    public interface IModuleLogger;
    public class ModuleLogger : IModuleLogger;

    public interface IModuleRepo;
    public class ModuleRepo : IModuleRepo;

    public class ModuleHandler;

    public class CrossModuleConsumer(IModuleLogger logger)
    {
        public IModuleLogger Logger => logger;
    }

    public class NestedHandler;

    // --- Modules ---

    [Singleton<IModuleLogger, ModuleLogger>]
    public class LoggingModule;

    [Scoped<IModuleRepo, ModuleRepo>]
    public class DataModule;

    // Nested module: includes LoggingModule + DataModule, adds its own service
    [UseModule<LoggingModule>]
    [UseModule<DataModule>]
    [Transient<NestedHandler>]
    public class CompositeModule;

    // Module with cross-module dependency (CrossModuleConsumer depends on IModuleLogger from LoggingModule)
    [Scoped<CrossModuleConsumer>]
    public class ConsumerModule;

    // --- Service providers ---

    [ServiceProvider]
    [UseModule<LoggingModule>]
    [UseModule<DataModule>]
    [Transient<ModuleHandler>]
    public partial class ModuleServiceProvider;

    [ServiceProvider]
    [UseModule<LoggingModule>]
    [UseModule<ConsumerModule>]
    public partial class CrossModuleDepsProvider;

    [ServiceProvider]
    [UseModule<CompositeModule>]
    public partial class NestedModuleProvider;
}
