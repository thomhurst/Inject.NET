using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Ninject;

namespace Benchmarks.Benchmarks.WrappedOpenGeneric;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("WrappedOpenGeneric")]
public class NinjectBenchmark
{
    private StandardKernel _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _serviceProvider = new StandardKernel();

        _serviceProvider.Bind<Class1>().ToSelf().InTransientScope();
        _serviceProvider.Bind<GenericWrapper>().ToSelf().InTransientScope();
        _serviceProvider.Bind(typeof(IGenericInterface<>)).To(typeof(GenericClass<>)).InTransientScope();
    }

    [Benchmark]
    public void GetDependency()
    {
        using var scope = _serviceProvider.BeginBlock();

        scope.Get<GenericWrapper>();
    }
}