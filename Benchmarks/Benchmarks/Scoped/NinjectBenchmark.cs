using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Ninject;

namespace Benchmarks.Benchmarks.Scoped;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("Scoped")]
public class NinjectBenchmark
{
    private object? _scope;
    private IKernel _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _serviceProvider = new StandardKernel();
    }

    [IterationSetup]
    public void Setup()
    {
        _scope = new object();
        _serviceProvider.Bind<Class1>().ToSelf().InScope(_ => _scope);
    }

    [Benchmark]
    public void GetDependency()
    {
        using var scope = _serviceProvider.BeginBlock();

        scope.Get<Class1>();
    }
}