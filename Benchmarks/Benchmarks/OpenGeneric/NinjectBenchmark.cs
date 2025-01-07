using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Ninject;

namespace Benchmarks.Benchmarks.OpenGeneric;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("OpenGeneric")]
public class NinjectBenchmark
{
    private IKernel _serviceProvider = null!;

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

        scope.Get<IGenericInterface<GenericWrapper>>();
    }
}