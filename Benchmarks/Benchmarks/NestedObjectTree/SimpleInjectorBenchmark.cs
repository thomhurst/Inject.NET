using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Benchmarks.Benchmarks.NestedObjectTree;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("NestedObjectTree")]
public class SimpleInjectorBenchmark
{
    private Container _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _serviceProvider = new Container();

        _serviceProvider.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

        _serviceProvider.Register<Interface1, Class1>(Lifestyle.Transient);
        _serviceProvider.Register<Interface2, Class2>(Lifestyle.Transient);
        _serviceProvider.Register<Interface3, Class3>(Lifestyle.Transient);
        _serviceProvider.Register<Interface4, Class4>(Lifestyle.Transient);
        _serviceProvider.Register<Interface5, Class5>(Lifestyle.Transient);
    }

    [Benchmark]
    public async Task GetDependency()
    {
        await using var scope = AsyncScopedLifestyle.BeginScope(_serviceProvider);
        
        scope.Container!.GetInstance<Interface5>();
    }
}