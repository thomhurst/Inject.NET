using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Ninject;

namespace Benchmarks.Benchmarks.SingletonWithInterface;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("SingletonWithInterface")]
public class NinjectBenchmark
{
    private IKernel _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _serviceProvider = new StandardKernel()
            .Bind<Interface1>().To<Class1>().InSingletonScope()
            .Kernel!;
    }

    [Benchmark]
    public void GetDependency()
    {
        using var scope = _serviceProvider.BeginBlock();

        scope.Get<Interface1>();
    }
}