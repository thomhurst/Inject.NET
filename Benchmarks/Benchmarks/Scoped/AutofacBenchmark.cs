using Autofac;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;

namespace Benchmarks.Benchmarks.Scoped;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("Scoped")]
public class AutofacBenchmark
{
    private IContainer _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.RegisterType<Class1>().InstancePerLifetimeScope();

        _serviceProvider = containerBuilder.Build();
    }

    [Benchmark]
    public async Task GetDependency()
    {
        await using var scope = _serviceProvider.BeginLifetimeScope();

        scope.Resolve<Class1>();
    }
}