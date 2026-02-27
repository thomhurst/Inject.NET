using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.Models;

namespace Inject.NET.Tests;

/// <summary>
/// Tests for conditional service registration with predicates.
/// Demonstrates runtime conditional registration via the ConfigureServices pattern.
/// </summary>
public partial class ConditionalRegistrationTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Basic conditional registration
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task ConditionalSingleton_WhenPredicateMatches_ReturnsConditionalService()
    {
        await using var serviceProvider = await ConditionalSingletonServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ILogger>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<SpecialLogger>();
    }

    [Test]
    public async Task ConditionalSingleton_WhenPredicateDoesNotMatch_ReturnsFallback()
    {
        await using var serviceProvider = await ConditionalFallbackServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ILogger>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<ConsoleLogger>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Multiple conditions with different predicates
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task MultipleConditions_FirstMatchingPredicateWins()
    {
        await using var serviceProvider = await MultiConditionServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ILogger>();

        // The last registered predicate that matches wins (DebugLogger has ServiceType == ILogger which matches)
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<DebugLogger>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Services without predicates work as before (regression test)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task NonConditionalRegistration_WorksAsBeforeWithoutPredicates()
    {
        await using var serviceProvider = await NonConditionalServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ILogger>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<ConsoleLogger>();
    }

    [Test]
    public async Task NonConditionalRegistration_MultipleRegistrations_LastOneWins()
    {
        await using var serviceProvider = await MultiNonConditionalServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ILogger>();

        // Last registered non-conditional wins
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<FileLogger>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Conditional scoped registration
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task ConditionalScoped_WhenPredicateMatches_ReturnsConditionalService()
    {
        await using var serviceProvider = await ConditionalScopedServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ILogger>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<SpecialLogger>();
    }

    [Test]
    public async Task ConditionalScoped_WhenPredicateDoesNotMatch_ReturnsFallback()
    {
        await using var serviceProvider = await ConditionalScopedFallbackServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ILogger>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<ConsoleLogger>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Conditional transient registration
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task ConditionalTransient_WhenPredicateMatches_ReturnsConditionalService()
    {
        await using var serviceProvider = await ConditionalTransientServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ILogger>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<SpecialLogger>();
    }

    [Test]
    public async Task ConditionalTransient_WhenPredicateDoesNotMatch_ReturnsFallback()
    {
        await using var serviceProvider = await ConditionalTransientFallbackServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ILogger>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<ConsoleLogger>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Conditional with factory delegates
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task ConditionalSingleton_WithFactory_WhenPredicateMatches()
    {
        await using var serviceProvider = await ConditionalFactoryServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ILogger>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<SpecialLogger>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ConditionalContext provides correct information
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task ConditionalContext_HasCorrectServiceType()
    {
        _capturedContext = null;

        await using var serviceProvider = await ConditionalContextServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ILogger>();

        await Assert.That(_capturedContext).IsNotNull();
        await Assert.That(_capturedContext!.ServiceType).IsEqualTo(typeof(ILogger));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Mixed conditional and non-conditional
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task MixedConditionalAndNonConditional_ConditionalMatchesFirst()
    {
        await using var serviceProvider = await MixedConditionalServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ILogger>();

        // SpecialLogger has a predicate that matches, and it's registered after ConsoleLogger
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<SpecialLogger>();
    }

    [Test]
    public async Task MixedConditionalAndNonConditional_FallsBackToNonConditional()
    {
        await using var serviceProvider = await MixedConditionalFallbackServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<ILogger>();

        // SpecialLogger predicate doesn't match, falls back to ConsoleLogger (no predicate)
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<ConsoleLogger>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Service Provider Definitions
    // ═══════════════════════════════════════════════════════════════════════

    [ServiceProvider]
    public partial class ConditionalSingletonServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<ILogger, ConsoleLogger>();
                this.AddSingleton<ILogger, SpecialLogger>(
                    predicate: _ => true); // Always matches
            }
        }
    }

    [ServiceProvider]
    public partial class ConditionalFallbackServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<ILogger, ConsoleLogger>();
                this.AddSingleton<ILogger, SpecialLogger>(
                    predicate: _ => false); // Never matches, falls back to ConsoleLogger
            }
        }
    }

    [ServiceProvider]
    public partial class MultiConditionServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<ILogger, ConsoleLogger>(); // fallback
                this.AddSingleton<ILogger, FileLogger>(
                    predicate: _ => false); // Does not match
                this.AddSingleton<ILogger, DebugLogger>(
                    predicate: ctx => ctx.ServiceType == typeof(ILogger)); // Matches
            }
        }
    }

    [ServiceProvider]
    public partial class NonConditionalServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<ILogger, ConsoleLogger>();
            }
        }
    }

    [ServiceProvider]
    public partial class MultiNonConditionalServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<ILogger, ConsoleLogger>();
                this.AddSingleton<ILogger, FileLogger>();
            }
        }
    }

    [ServiceProvider]
    public partial class ConditionalScopedServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddScoped<ILogger, ConsoleLogger>();
                this.AddScoped<ILogger, SpecialLogger>(
                    predicate: _ => true);
            }
        }
    }

    [ServiceProvider]
    public partial class ConditionalScopedFallbackServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddScoped<ILogger, ConsoleLogger>();
                this.AddScoped<ILogger, SpecialLogger>(
                    predicate: _ => false);
            }
        }
    }

    [ServiceProvider]
    public partial class ConditionalTransientServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddTransient<ILogger, ConsoleLogger>();
                this.AddTransient<ILogger, SpecialLogger>(
                    predicate: _ => true);
            }
        }
    }

    [ServiceProvider]
    public partial class ConditionalTransientFallbackServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddTransient<ILogger, ConsoleLogger>();
                this.AddTransient<ILogger, SpecialLogger>(
                    predicate: _ => false);
            }
        }
    }

    [ServiceProvider]
    public partial class ConditionalFactoryServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<ILogger>(scope => new ConsoleLogger());
                this.AddSingleton<ILogger>(
                    scope => new SpecialLogger(),
                    predicate: _ => true);
            }
        }
    }

    // Static field to capture context for testing
    private static ConditionalContext? _capturedContext;

    [ServiceProvider]
    public partial class ConditionalContextServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<ILogger, ConsoleLogger>();
                this.AddSingleton<ILogger, SpecialLogger>(
                    predicate: ctx =>
                    {
                        _capturedContext = ctx;
                        return true;
                    });
            }
        }
    }

    [ServiceProvider]
    public partial class MixedConditionalServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<ILogger, ConsoleLogger>(); // non-conditional fallback
                this.AddSingleton<ILogger, SpecialLogger>(
                    predicate: _ => true); // conditional, matches
            }
        }
    }

    [ServiceProvider]
    public partial class MixedConditionalFallbackServiceProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddSingleton<ILogger, ConsoleLogger>(); // non-conditional fallback
                this.AddSingleton<ILogger, SpecialLogger>(
                    predicate: _ => false); // conditional, does not match
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Test Services
    // ═══════════════════════════════════════════════════════════════════════

    public interface ILogger { }
    public class ConsoleLogger : ILogger { }
    public class FileLogger : ILogger { }
    public class SpecialLogger : ILogger { }
    public class DebugLogger : ILogger { }
}
