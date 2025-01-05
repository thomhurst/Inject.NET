using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Microsoft.Extensions.DependencyInjection;
using Ninject;

namespace Benchmarks;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
public class NinjectBenchmark
{
    private IKernel _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _serviceProvider = new StandardKernel()
            .Bind<Interface1>().To<Class1>().InSingletonScope()
            .Kernel!.Bind<Interface2>().To<Class2>().InSingletonScope()
            .Kernel!.Bind<Interface3>().To<Class3>().InSingletonScope()
            .Kernel!.Bind<Interface4>().To<Class4>().InTransientScope()
            .Kernel!.Bind<Interface5>().To<Class5>().InScope(ctx => ctx.GetScope())
            .Kernel!;
    }

    [Benchmark]
    public async Task GetDependency()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<Interface5>();
    }
}