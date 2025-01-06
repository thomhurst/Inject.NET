using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Ninject;

namespace Benchmarks.Benchmarks.NestedObjectTree;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("NestedObjectTree")]
public class NinjectBenchmark
{
    private IKernel _serviceProvider = null!;
    private object? _scope;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _serviceProvider = new StandardKernel()
            .Bind<Interface1>().To<Class1>().InTransientScope()
            .Kernel!.Bind<Interface2>().To<Class2>().InTransientScope()
            .Kernel!.Bind<Interface3>().To<Class3>().InTransientScope()
            .Kernel!.Bind<Interface4>().To<Class4>().InTransientScope()
            .Kernel!.Bind<Interface5>().To<Class5>().InTransientScope()
            .Kernel!;
    }

    [Benchmark]
    public void GetDependency()
    {
        using var scope = _serviceProvider.BeginBlock();

        scope.Get<Interface5>();
    }
}