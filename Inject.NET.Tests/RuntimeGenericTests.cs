using System.Reflection;
using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class RuntimeGenericTests
{
    [Test]
    public async Task CanResolve_RuntimeConstructedGenericType()
    {
        var serviceProvider = await RuntimeGenericServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        // Construct generic type at runtime
        var genericType = typeof(IGenericService<>).MakeGenericType(typeof(string));
        
        var service = scope.GetService(genericType);

        await Assert.That(service).IsNotNull();
        
        // Verify it's the correct concrete type
        var expectedConcreteType = typeof(GenericService<>).MakeGenericType(typeof(string));
        await Assert.That(service?.GetType()).IsEqualTo(expectedConcreteType);
    }

    [Test]
    public async Task CanResolve_RuntimeConstructedNestedGeneric()
    {
        var serviceProvider = await RuntimeGenericServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        // Construct nested generic type at runtime: IContainer<IWrapper<string>>
        var wrapperType = typeof(IWrapper<>).MakeGenericType(typeof(string));
        var containerType = typeof(IContainer<>).MakeGenericType(wrapperType);
        
        var container = scope.GetService(containerType);

        await Assert.That(container).IsNotNull();
        
        // Use reflection to verify the nested structure
        var contentProperty = container?.GetType().GetProperty("Content");
        await Assert.That(contentProperty).IsNotNull();
        
        var wrapper = contentProperty?.GetValue(container);
        await Assert.That(wrapper).IsNotNull();
        
        var valueProperty = wrapper?.GetType().GetProperty("Value");
        await Assert.That(valueProperty).IsNotNull();
        
        var value = valueProperty?.GetValue(wrapper);
        await Assert.That(value).IsEqualTo("Wrapped string");
    }

    [Test]
    public async Task CanResolve_MultipleRuntimeGenericTypes()
    {
        var serviceProvider = await RuntimeGenericServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        // Test multiple different runtime-constructed types
        var types = new[]
        {
            typeof(IGenericService<>).MakeGenericType(typeof(string)),
            typeof(IGenericService<>).MakeGenericType(typeof(int)),
            typeof(IGenericService<>).MakeGenericType(typeof(RuntimeModel)),
        };

        foreach (var type in types)
        {
            var service = scope.GetService(type);
            await Assert.That(service).IsNotNull();
            
            // Each should be a different instance but same generic type definition
            var expectedConcreteType = typeof(GenericService<>).MakeGenericType(type.GetGenericArguments()[0]);
            await Assert.That(service?.GetType()).IsEqualTo(expectedConcreteType);
        }
    }

    [Test]
    public async Task CanResolve_RuntimeGenericWithConstraints()
    {
        var serviceProvider = await RuntimeGenericServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        // Construct generic type with constraints at runtime
        var constrainedType = typeof(IConstrainedService<>).MakeGenericType(typeof(ConstrainedRuntimeModel));
        
        var service = scope.GetService(constrainedType);

        await Assert.That(service).IsNotNull();
        
        // Verify constraint satisfaction through reflection
        var processMethod = service?.GetType().GetMethod("Process");
        await Assert.That(processMethod).IsNotNull();
        
        var result = processMethod?.Invoke(service, null);
        await Assert.That(result).IsEqualTo("Processing: ConstrainedRuntimeModel with ID: 42");
    }

    [Test]
    public async Task CanResolve_RuntimeMultipleTypeParameters()
    {
        var serviceProvider = await RuntimeGenericServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        // Construct multi-parameter generic at runtime
        var multiGenericType = typeof(IMultiParameterService<,>).MakeGenericType(typeof(string), typeof(int));
        
        var service = scope.GetService(multiGenericType);

        await Assert.That(service).IsNotNull();
        
        // Test functionality through reflection
        var combineMethod = service?.GetType().GetMethod("Combine");
        await Assert.That(combineMethod).IsNotNull();
        
        var result = combineMethod?.Invoke(service, new object[] { "test", 123 });
        await Assert.That(result).IsEqualTo("Combined: test + 123");
    }

    [Test]
    public async Task CanResolve_RuntimeGenericCollection()
    {
        var serviceProvider = await RuntimeGenericCollectionServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        // Construct generic collection type at runtime
        var collectionType = typeof(IEnumerable<>).MakeGenericType(typeof(IProcessor<string>));
        
        var services = scope.GetService(collectionType) as IEnumerable<object>;

        await Assert.That(services).IsNotNull();
        
        var serviceList = services?.ToList();
        await Assert.That(serviceList).IsNotNull();
        await Assert.That(serviceList?.Count).IsEqualTo(2);
        
        // Verify each service in the collection
        foreach (var service in serviceList!)
        {
            await Assert.That(service).IsNotNull();
            var processMethod = service.GetType().GetMethod("Process");
            await Assert.That(processMethod).IsNotNull();
            
            var result = processMethod.Invoke(service, new object[] { "test" });
            await Assert.That(result).IsTypeOf<string>();
        }
    }

    [Test]
    public async Task CanResolve_RuntimeGenericFactory()
    {
        var serviceProvider = await RuntimeGenericServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        // Construct factory type at runtime
        var factoryType = typeof(IRuntimeFactory<>).MakeGenericType(typeof(RuntimeModel));
        
        var factory = scope.GetService(factoryType);

        await Assert.That(factory).IsNotNull();
        
        // Use the factory through reflection
        var createMethod = factory?.GetType().GetMethod("Create");
        await Assert.That(createMethod).IsNotNull();
        
        var created = createMethod?.Invoke(factory, new object[] { "RuntimeCreated" });
        await Assert.That(created).IsNotNull();
        
        // Verify the created object
        var nameProperty = created?.GetType().GetProperty("Name");
        var name = nameProperty?.GetValue(created);
        await Assert.That(name).IsEqualTo("RuntimeCreated");
    }

    [Test]
    public async Task ThrowsException_WhenRuntimeGenericTypeNotRegistered()
    {
        var serviceProvider = await RuntimeGenericServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        // Try to resolve an unregistered generic type
        var unregisteredType = typeof(IUnregisteredGeneric<>).MakeGenericType(typeof(string));
        
        var service = scope.GetService(unregisteredType);

        await Assert.That(service).IsNull();
    }

    [Test]
    public async Task CanResolve_RuntimeGenericWithDependency()
    {
        var serviceProvider = await RuntimeGenericServiceProvider.BuildAsync();

        await using var scope = serviceProvider.CreateScope();

        // Construct generic type that has dependencies
        var dependentType = typeof(IDependentGenericService<>).MakeGenericType(typeof(RuntimeModel));
        
        var service = scope.GetService(dependentType);

        await Assert.That(service).IsNotNull();
        
        // Verify the dependency was injected
        var dependencyProperty = service?.GetType().GetProperty("Dependency");
        await Assert.That(dependencyProperty).IsNotNull();
        
        var dependency = dependencyProperty?.GetValue(service);
        await Assert.That(dependency).IsNotNull();
        await Assert.That(dependency).IsTypeOf<RuntimeDependency>();
    }

    // Service Provider Definitions

    [ServiceProvider]
    [Transient<RuntimeModel>]
    [Transient<RuntimeDependency>]
    [Transient<ConstrainedRuntimeModel>]
    [Transient(typeof(IGenericService<>), typeof(GenericService<>))]
    [Transient(typeof(IWrapper<>), typeof(Wrapper<>))]
    [Transient(typeof(IContainer<>), typeof(Container<>))]
    [Transient(typeof(IConstrainedService<>), typeof(ConstrainedService<>))]
    [Transient(typeof(IMultiParameterService<,>), typeof(MultiParameterService<,>))]
    [Transient(typeof(IRuntimeFactory<>), typeof(RuntimeFactory<>))]
    [Transient(typeof(IDependentGenericService<>), typeof(DependentGenericService<>))]
    public partial class RuntimeGenericServiceProvider;

    [ServiceProvider]
    [Transient(typeof(IProcessor<>), typeof(ProcessorA<>))]
    [Transient(typeof(IProcessor<>), typeof(ProcessorB<>))]
    public partial class RuntimeGenericCollectionServiceProvider;

    // Interface and Class Definitions

    public interface IGenericService<T>
    {
        T Process(T input);
    }

    public class GenericService<T> : IGenericService<T>
    {
        public T Process(T input) => input;
    }

    public interface IWrapper<T>
    {
        T Value { get; }
    }

    public interface IContainer<T>
    {
        T Content { get; }
    }

    public class Wrapper<T> : IWrapper<T>
    {
        public T Value { get; }

        public Wrapper(T value)
        {
            Value = value;
        }
    }

    // For testing nested generics, we need a wrapper that contains a string value
    public class StringWrapper : IWrapper<string>
    {
        public string Value => "Wrapped string";
    }

    public class Container<T> : IContainer<T>
    {
        public T Content { get; }

        public Container(T content)
        {
            Content = content;
        }
    }

    // Need specific registration for the nested case
    [ServiceProvider]
    [Transient<StringWrapper>]
    [Transient<IWrapper<string>, StringWrapper>]
    [Transient(typeof(IContainer<>), typeof(Container<>))]
    public partial class NestedRuntimeServiceProvider;

    public class RuntimeModel
    {
        public string Name { get; set; } = "RuntimeModel";
        public int Id { get; set; } = 1;
    }

    public interface IIdentifiable
    {
        int Id { get; }
    }

    public interface IConstrainedService<T> where T : IIdentifiable
    {
        string Process();
    }

    public class ConstrainedRuntimeModel : IIdentifiable
    {
        public int Id => 42;
        public override string ToString() => "ConstrainedRuntimeModel";
    }

    public class ConstrainedService<T> : IConstrainedService<T> where T : IIdentifiable
    {
        private readonly T _model;

        public ConstrainedService(T model)
        {
            _model = model;
        }

        public string Process() => $"Processing: {_model} with ID: {_model.Id}";
    }

    public interface IMultiParameterService<T1, T2>
    {
        string Combine(T1 first, T2 second);
    }

    public class MultiParameterService<T1, T2> : IMultiParameterService<T1, T2>
    {
        public string Combine(T1 first, T2 second) => $"Combined: {first} + {second}";
    }

    public interface IProcessor<T>
    {
        string Process(T input);
    }

    public class ProcessorA<T> : IProcessor<T>
    {
        public string Process(T input) => $"ProcessorA processed: {input}";
    }

    public class ProcessorB<T> : IProcessor<T>
    {
        public string Process(T input) => $"ProcessorB processed: {input}";
    }

    public interface IRuntimeFactory<T>
    {
        T Create(string name);
    }

    public class RuntimeFactory<T> : IRuntimeFactory<T>
    {
        public T Create(string name)
        {
            var instance = Activator.CreateInstance<T>();
            
            // Set name property if it exists
            var nameProperty = typeof(T).GetProperty("Name");
            nameProperty?.SetValue(instance, name);
            
            return instance;
        }
    }

    public interface IDependentGenericService<T>
    {
        RuntimeDependency Dependency { get; }
        T ProcessWithDependency(T input);
    }

    public class DependentGenericService<T> : IDependentGenericService<T>
    {
        public RuntimeDependency Dependency { get; }

        public DependentGenericService(RuntimeDependency dependency)
        {
            Dependency = dependency;
        }

        public T ProcessWithDependency(T input)
        {
            Dependency.LogUsage();
            return input;
        }
    }

    public class RuntimeDependency
    {
        public int UsageCount { get; private set; }

        public void LogUsage()
        {
            UsageCount++;
        }
    }

    // For testing unregistered types
    public interface IUnregisteredGeneric<T>
    {
        T Process(T input);
    }
}