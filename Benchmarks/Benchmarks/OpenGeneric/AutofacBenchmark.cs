using Autofac;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;

namespace Benchmarks.Benchmarks.OpenGeneric;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("OpenGeneric")]
public class AutofacBenchmark
{
    private IContainer _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder
            .RegisterType<Class1>().InstancePerDependency();

        containerBuilder.RegisterType<GenericWrapper>().InstancePerDependency();
            
        containerBuilder
            .RegisterType(typeof(GenericClass<>)).As(typeof(IGenericInterface<>)).InstancePerDependency();

        _serviceProvider = containerBuilder.Build();
    }

    [Benchmark]
    public async Task GetDependency()
    {
        await using var scope = _serviceProvider.BeginLifetimeScope();

        scope.Resolve<GenericWrapper>();
    }
}