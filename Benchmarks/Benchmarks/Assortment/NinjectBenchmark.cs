using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Ninject;

namespace Benchmarks.Benchmarks.Assortment;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("Assortment")]
public class NinjectBenchmark
{
    private StandardKernel _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _serviceProvider = new StandardKernel();
        _serviceProvider.Bind<Interface1>().To<Class1>().InSingletonScope();
        _serviceProvider.Bind<Interface2>().To<Class2>().InSingletonScope();
        _serviceProvider.Bind<Interface3>().To<Class3>().InSingletonScope();
        _serviceProvider.Bind<Interface4>().To<Class4>().InTransientScope();
        _serviceProvider.Bind<Interface5>().To<Class5>().InThreadScope();
    }

    [Benchmark]
    public void GetDependency()
    {
        using var scope = _serviceProvider.BeginBlock();

        scope.Get<Interface5>();
    }
}