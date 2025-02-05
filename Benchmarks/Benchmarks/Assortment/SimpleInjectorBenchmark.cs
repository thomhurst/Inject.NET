using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Benchmarks.Benchmarks.Assortment;

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

        _serviceProvider.RegisterSingleton<Interface1, Class1>();
        _serviceProvider.RegisterSingleton<Interface2, Class2>();
        _serviceProvider.RegisterSingleton<Interface3, Class3>();
        _serviceProvider.Register<Interface4, Class4>(Lifestyle.Scoped);
        _serviceProvider.Register<Interface5, Class5>(Lifestyle.Transient);
    }

    [Benchmark]
    public async Task GetDependency()
    {
        await using var scope = AsyncScopedLifestyle.BeginScope(_serviceProvider);
        
        scope.Container!.GetInstance<Interface5>();
    }
}