using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Ninject;

namespace Benchmarks.Benchmarks.ScopedWithInterface;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("ScopedWithInterface")]
public class NinjectBenchmark
{
    private object? _scope;
    private IKernel _serviceProvider = null!;

    [IterationSetup]
    public void Setup()
    {
        _scope = new object();
        _serviceProvider.Bind<Interface1>().To<Class1>().InScope(_ => _scope);
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _serviceProvider = new StandardKernel();
    }

    [Benchmark]
    public void GetDependency()
    {
        using var scope = _serviceProvider.BeginBlock();

        scope.Get<Interface1>();
    }
}