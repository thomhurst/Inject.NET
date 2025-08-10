using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class DecoratorTests
{
    [Test]
    public async Task SingleDecorator_WrapsOriginalService()
    {
        await using var serviceProvider = await SingleDecoratorServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var logger = scope.GetRequiredService<ILogger>();
        
        await Assert.That(logger).IsTypeOf<TimestampLoggerDecorator>();
        logger.Log("Test message");
        
        var decorator = (TimestampLoggerDecorator)logger;
        await Assert.That(decorator.Inner).IsTypeOf<ConsoleLogger>();
    }

    [Test]
    public async Task MultipleDecorators_AppliedInOrder()
    {
        await using var serviceProvider = await MultipleDecoratorsServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var logger = scope.GetRequiredService<ILogger>();
        
        // Outermost decorator
        await Assert.That(logger).IsTypeOf<FileLoggerDecorator>();
        
        var fileDecorator = (FileLoggerDecorator)logger;
        await Assert.That(fileDecorator.Inner).IsTypeOf<TimestampLoggerDecorator>();
        
        var timestampDecorator = (TimestampLoggerDecorator)fileDecorator.Inner;
        await Assert.That(timestampDecorator.Inner).IsTypeOf<ConsoleLogger>();
        
        logger.Log("Test message");
    }

    [Test]
    public async Task DecoratorWithDifferentLifetimes_SingletonDecorator()
    {
        await using var serviceProvider = await MixedLifetimeServiceProvider.BuildAsync();
        
        await using var scope1 = serviceProvider.CreateScope();
        var repo1 = scope1.GetRequiredService<IRepository>();
        
        await using var scope2 = serviceProvider.CreateScope();
        var repo2 = scope2.GetRequiredService<IRepository>();
        
        // Singleton decorator should be the same instance
        await Assert.That(repo1).IsSameReferenceAs(repo2);
        
        // But the inner scoped service should be different
        var decorator1 = (CachingRepositoryDecorator)repo1;
        var decorator2 = (CachingRepositoryDecorator)repo2;
        
        await Assert.That(decorator1.Inner).IsNotSameReferenceAs(decorator2.Inner);
    }

    [Test]
    public async Task TransientDecorators_NewInstanceEachTime()
    {
        await using var serviceProvider = await TransientDecoratorServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var command1 = scope.GetRequiredService<ICommand>();
        var command2 = scope.GetRequiredService<ICommand>();
        
        // Both decorator and inner service should be different instances
        await Assert.That(command1).IsNotSameReferenceAs(command2);
        
        var decorator1 = (LoggingCommandDecorator)command1;
        var decorator2 = (LoggingCommandDecorator)command2;
        
        await Assert.That(decorator1.Inner).IsNotSameReferenceAs(decorator2.Inner);
    }

    [Test]
    public async Task DecoratorWithOrderProperty_AppliedInCorrectOrder()
    {
        await using var serviceProvider = await OrderedDecoratorsServiceProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var service = scope.GetRequiredService<IService>();
        
        // Decorator3 (Order=30) should be outermost
        await Assert.That(service).IsTypeOf<Decorator3>();
        
        var decorator3 = (Decorator3)service;
        // Decorator2 (Order=20) should be next
        await Assert.That(decorator3.Inner).IsTypeOf<Decorator2>();
        
        var decorator2 = (Decorator2)decorator3.Inner;
        // Decorator1 (Order=10) should be next
        await Assert.That(decorator2.Inner).IsTypeOf<Decorator1>();
        
        var decorator1 = (Decorator1)decorator2.Inner;
        // BaseService should be innermost
        await Assert.That(decorator1.Inner).IsTypeOf<BaseService>();
    }

    // Service Providers

    [ServiceProvider]
    [Singleton<ILogger, ConsoleLogger>]
    [SingletonDecorator<ILogger, TimestampLoggerDecorator>]
    public partial class SingleDecoratorServiceProvider;

    [ServiceProvider]
    [Singleton<ILogger, ConsoleLogger>]
    [SingletonDecorator<ILogger, TimestampLoggerDecorator>]
    [SingletonDecorator<ILogger, FileLoggerDecorator>]
    public partial class MultipleDecoratorsServiceProvider;

    [ServiceProvider]
    [Scoped<IRepository, SqlRepository>]
    [SingletonDecorator<IRepository, CachingRepositoryDecorator>]
    public partial class MixedLifetimeServiceProvider;

    [ServiceProvider]
    [Transient<ICommand, SaveCommand>]
    [TransientDecorator<ICommand, LoggingCommandDecorator>]
    public partial class TransientDecoratorServiceProvider;

    [ServiceProvider]
    [Singleton<IService, BaseService>]
    [SingletonDecorator<IService, Decorator1>(Order = 10)]
    [SingletonDecorator<IService, Decorator2>(Order = 20)]
    [SingletonDecorator<IService, Decorator3>(Order = 30)]
    public partial class OrderedDecoratorsServiceProvider;

    // Interfaces and Implementations

    public interface ILogger
    {
        void Log(string message);
    }

    public class ConsoleLogger : ILogger
    {
        public void Log(string message) => Console.WriteLine(message);
    }

    public class TimestampLoggerDecorator : ILogger
    {
        public ILogger Inner { get; }
        
        public TimestampLoggerDecorator(ILogger inner)
        {
            Inner = inner;
        }
        
        public void Log(string message)
        {
            Inner.Log($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
        }
    }

    public class FileLoggerDecorator : ILogger
    {
        public ILogger Inner { get; }
        private readonly string _fileName = "log.txt";
        
        public FileLoggerDecorator(ILogger inner)
        {
            Inner = inner;
        }
        
        public void Log(string message)
        {
            Inner.Log(message);
            // Also log to file (simplified for test)
            Console.WriteLine($"[FILE: {_fileName}] {message}");
        }
    }

    public interface IRepository
    {
        void Save(string data);
    }

    public class SqlRepository : IRepository
    {
        public void Save(string data) => Console.WriteLine($"Saving to SQL: {data}");
    }

    public class CachingRepositoryDecorator : IRepository
    {
        public IRepository Inner { get; }
        private readonly Dictionary<string, bool> _cache = new();
        
        public CachingRepositoryDecorator(IRepository inner)
        {
            Inner = inner;
        }
        
        public void Save(string data)
        {
            if (!_cache.ContainsKey(data))
            {
                Inner.Save(data);
                _cache[data] = true;
            }
        }
    }

    public interface ICommand
    {
        void Execute();
    }

    public class SaveCommand : ICommand
    {
        public void Execute() => Console.WriteLine("Executing save command");
    }

    public class LoggingCommandDecorator : ICommand
    {
        public ICommand Inner { get; }
        
        public LoggingCommandDecorator(ICommand inner)
        {
            Inner = inner;
        }
        
        public void Execute()
        {
            Console.WriteLine("Before command execution");
            Inner.Execute();
            Console.WriteLine("After command execution");
        }
    }

    public interface IService
    {
        void DoWork();
    }

    public class BaseService : IService
    {
        public void DoWork() => Console.WriteLine("Base service working");
    }

    public class Decorator1 : IService
    {
        public IService Inner { get; }
        
        public Decorator1(IService inner)
        {
            Inner = inner;
        }
        
        public void DoWork()
        {
            Console.WriteLine("Decorator1 before");
            Inner.DoWork();
            Console.WriteLine("Decorator1 after");
        }
    }

    public class Decorator2 : IService
    {
        public IService Inner { get; }
        
        public Decorator2(IService inner)
        {
            Inner = inner;
        }
        
        public void DoWork()
        {
            Console.WriteLine("Decorator2 before");
            Inner.DoWork();
            Console.WriteLine("Decorator2 after");
        }
    }

    public class Decorator3 : IService
    {
        public IService Inner { get; }
        
        public Decorator3(IService inner)
        {
            Inner = inner;
        }
        
        public void DoWork()
        {
            Console.WriteLine("Decorator3 before");
            Inner.DoWork();
            Console.WriteLine("Decorator3 after");
        }
    }
}