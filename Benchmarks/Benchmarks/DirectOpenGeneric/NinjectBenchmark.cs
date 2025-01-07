using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Ninject;

namespace Benchmarks.Benchmarks.DirectOpenGeneric;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("DirectOpenGeneric")]
public class NinjectBenchmark
{
    private StandardKernel _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _serviceProvider = new StandardKernel();

        _serviceProvider.Bind<Class1>().ToSelf().InTransientScope();
        _serviceProvider.Bind(typeof(IGenericInterface<>)).To(typeof(GenericClass<>)).InTransientScope();
    }

    [Benchmark]
    public void GetDependency()
    {
        using var scope = _serviceProvider.BeginBlock();

        scope.Get<IGenericInterface<Class1>>();
    }
}