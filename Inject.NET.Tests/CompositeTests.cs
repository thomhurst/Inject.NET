using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class CompositeTests
{
    [Test]
    public async Task Composite_IsReturnedWhenResolvingSingleService()
    {
        await using var serviceProvider = await CompositeServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var sender = scope.GetRequiredService<INotificationSender>();

        await Assert.That(sender).IsTypeOf<CompositeNotificationSender>();
    }

    [Test]
    public async Task Composite_IsExcludedFromEnumerableResolution()
    {
        await using var serviceProvider = await CompositeServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var senders = scope.GetServices<INotificationSender>().ToList();

        await Assert.That(senders).HasCount().EqualTo(2);
        await Assert.That(senders[0]).IsTypeOf<EmailSender>();
        await Assert.That(senders[1]).IsTypeOf<SmsSender>();
    }

    [Test]
    public async Task Composite_ReceivesAllOtherImplementations()
    {
        await using var serviceProvider = await CompositeServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var sender = scope.GetRequiredService<INotificationSender>();
        var composite = (CompositeNotificationSender)sender;

        await Assert.That(composite.Senders.Count).IsEqualTo(2);
        await Assert.That(composite.Senders[0]).IsTypeOf<EmailSender>();
        await Assert.That(composite.Senders[1]).IsTypeOf<SmsSender>();
    }

    [Test]
    public async Task Composite_WithScopedServices()
    {
        await using var serviceProvider = await ScopedCompositeServiceProvider.BuildAsync();

        await using var scope1 = serviceProvider.CreateScope();
        var handler1 = scope1.GetRequiredService<IHandler>();

        await using var scope2 = serviceProvider.CreateScope();
        var handler2 = scope2.GetRequiredService<IHandler>();

        await Assert.That(handler1).IsTypeOf<CompositeHandler>();
        await Assert.That(handler2).IsTypeOf<CompositeHandler>();

        // Different scopes should get different composite instances
        await Assert.That(handler1).IsNotSameReferenceAs(handler2);
    }

    [Test]
    public async Task Composite_WithScopedServices_EnumerableExcludesComposite()
    {
        await using var serviceProvider = await ScopedCompositeServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var handlers = scope.GetServices<IHandler>().ToList();

        await Assert.That(handlers).HasCount().EqualTo(2);
        await Assert.That(handlers[0]).IsTypeOf<HandlerA>();
        await Assert.That(handlers[1]).IsTypeOf<HandlerB>();
    }

    [Test]
    public async Task Composite_WithNonGenericAttribute()
    {
        await using var serviceProvider = await NonGenericCompositeServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var sender = scope.GetRequiredService<INotificationSender>();

        await Assert.That(sender).IsTypeOf<CompositeNotificationSender>();

        var senders = scope.GetServices<INotificationSender>().ToList();
        await Assert.That(senders).HasCount().EqualTo(2);
    }

    [Test]
    public async Task Composite_WithTransientServices()
    {
        await using var serviceProvider = await TransientCompositeServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var processor1 = scope.GetRequiredService<IProcessor>();
        var processor2 = scope.GetRequiredService<IProcessor>();

        await Assert.That(processor1).IsTypeOf<CompositeProcessor>();
        await Assert.That(processor2).IsTypeOf<CompositeProcessor>();

        // Transient services should be new instances each time
        await Assert.That(processor1).IsNotSameReferenceAs(processor2);
    }

    [Test]
    public async Task Composite_WithTransientServices_EnumerableExcludesComposite()
    {
        await using var serviceProvider = await TransientCompositeServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var processors = scope.GetServices<IProcessor>().ToList();

        await Assert.That(processors).HasCount().EqualTo(2);
        await Assert.That(processors[0]).IsTypeOf<ProcessorA>();
        await Assert.That(processors[1]).IsTypeOf<ProcessorB>();
    }

    // Service Providers

    [ServiceProvider]
    [Singleton<INotificationSender, EmailSender>]
    [Singleton<INotificationSender, SmsSender>]
    [Composite<INotificationSender, CompositeNotificationSender>]
    public partial class CompositeServiceProvider;

    [ServiceProvider]
    [Scoped<IHandler, HandlerA>]
    [Scoped<IHandler, HandlerB>]
    [Composite<IHandler, CompositeHandler>]
    public partial class ScopedCompositeServiceProvider;

    [ServiceProvider]
    [Singleton<INotificationSender, EmailSender>]
    [Singleton<INotificationSender, SmsSender>]
    [Composite(typeof(INotificationSender), typeof(CompositeNotificationSender))]
    public partial class NonGenericCompositeServiceProvider;

    [ServiceProvider]
    [Transient<IProcessor, ProcessorA>]
    [Transient<IProcessor, ProcessorB>]
    [Composite<IProcessor, CompositeProcessor>]
    public partial class TransientCompositeServiceProvider;

    // Interfaces and Implementations

    public interface INotificationSender
    {
        Task SendAsync(string message);
    }

    public class EmailSender : INotificationSender
    {
        public Task SendAsync(string message)
        {
            Console.WriteLine($"Email: {message}");
            return Task.CompletedTask;
        }
    }

    public class SmsSender : INotificationSender
    {
        public Task SendAsync(string message)
        {
            Console.WriteLine($"SMS: {message}");
            return Task.CompletedTask;
        }
    }

    public class CompositeNotificationSender : INotificationSender
    {
        public IReadOnlyList<INotificationSender> Senders { get; }

        public CompositeNotificationSender(IEnumerable<INotificationSender> senders)
        {
            Senders = senders.ToList();
        }

        public async Task SendAsync(string message)
        {
            foreach (var sender in Senders)
            {
                await sender.SendAsync(message);
            }
        }
    }

    // Scoped composite types

    public interface IHandler
    {
        void Handle();
    }

    public class HandlerA : IHandler
    {
        public void Handle() => Console.WriteLine("Handler A");
    }

    public class HandlerB : IHandler
    {
        public void Handle() => Console.WriteLine("Handler B");
    }

    public class CompositeHandler : IHandler
    {
        public IReadOnlyList<IHandler> Handlers { get; }

        public CompositeHandler(IEnumerable<IHandler> handlers)
        {
            Handlers = handlers.ToList();
        }

        public void Handle()
        {
            foreach (var handler in Handlers)
            {
                handler.Handle();
            }
        }
    }

    // Transient composite types

    public interface IProcessor
    {
        void Process();
    }

    public class ProcessorA : IProcessor
    {
        public void Process() => Console.WriteLine("Processor A");
    }

    public class ProcessorB : IProcessor
    {
        public void Process() => Console.WriteLine("Processor B");
    }

    public class CompositeProcessor : IProcessor
    {
        public IReadOnlyList<IProcessor> Processors { get; }

        public CompositeProcessor(IEnumerable<IProcessor> processors)
        {
            Processors = processors.ToList();
        }

        public void Process()
        {
            foreach (var processor in Processors)
            {
                processor.Process();
            }
        }
    }
}
