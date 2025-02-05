using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Benchmarks.Benchmarks.WrappedOpenGeneric;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("WrappedOpenGeneric")]
public class SimpleInjectorBenchmark
{
    private Container _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _serviceProvider = new Container();

        _serviceProvider.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

        _serviceProvider.Register<Class1>(Lifestyle.Transient);
        _serviceProvider.Register<GenericWrapper>(Lifestyle.Transient);
        _serviceProvider.Register(typeof(IGenericInterface<>), typeof(GenericClass<>), Lifestyle.Transient);
    }

    [Benchmark]
    public async Task GetDependency()
    {
        await using var scope = AsyncScopedLifestyle.BeginScope(_serviceProvider);
        
        scope.Container!.GetInstance<GenericWrapper>();
    }
}