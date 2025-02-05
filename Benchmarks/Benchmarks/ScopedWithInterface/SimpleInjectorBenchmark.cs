using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Benchmarks.Benchmarks.ScopedWithInterface;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("Assortment")]
public class SimpleInjectorBenchmark
{
    private Container _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _serviceProvider = new Container();

        _serviceProvider.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

        _serviceProvider.Register<Interface1, Class1>(Lifestyle.Scoped);
    }

    [Benchmark]
    public async Task GetDependency()
    {
        await using var scope = AsyncScopedLifestyle.BeginScope(_serviceProvider);
        
        scope.Container!.GetInstance<Interface1>();
    }
}